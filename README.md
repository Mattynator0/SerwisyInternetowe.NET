# IoT Sensor Dashboard (MQTT + MongoDB + Blockchain Rewards)

This project is a small IoT-like platform built with **.NET**, **MQTT**, **MongoDB** and an **Ethereum-based token reward** mechanism.

It consists of four Dockerized services:

- **mqtt** – MQTT broker (Eclipse Mosquitto)
- **mongo** – MongoDB database for sensor readings
- **backend** – ASP.NET Core web app (Razor Pages + REST API)  
  - subscribes to MQTT  
  - saves measurements to MongoDB  
  - optionally rewards sensors on Ethereum
- **datagenerator** – ASP.NET Core app simulating sensors and publishing to MQTT

---

## Class Overview

### DataGenerator project

- **`MqttService`**  
  Wrapper around `MQTTnet` client. Handles connecting to the MQTT broker and publishing JSON-serialized messages to specific topics.

- **`Sensor`**  
  Represents a single virtual sensor instance. Generates random values in a configured range at a given interval and publishes readings to MQTT on topic `sensors/{SensorType}/{SensorId}`.

- **`SensorGenerationSettings`**  
  Holds configuration for a sensor type: minimum and maximum generated value and the number of messages per minute. Exposes a calculated `MeasurementPublishIntervalMs`.

- **`ISensorSettingsProvider`**  
  Interface that defines a method for retrieving generation settings for all sensor types as a dictionary.

- **`SensorSettingsProvider`**  
  Reads sensor type settings from configuration (`appsettings` / environment variables) and falls back to built-in defaults for `light`, `temperature`, `air-quality` and `energy`.

- **`SensorRunner`**  
  Creates multiple `Sensor` instances per type (fixed number) using `SensorSettingsProvider` and starts infinite loops that periodically call `PublishMeasurementDataAsync` for each sensor.

- **`SensorsController`** (`[ApiController]`)  
  Minimal API controller with endpoint `POST /api/manual` that accepts manual sensor data (`SensorData` DTO) in JSON and forwards it to MQTT using `MqttService`.

- **`SensorData`** (in `SensorsController`)  
  Simple DTO used for manual sending: contains `SensorId`, `SensorType`, `Value` and `Timestamp` fields, which are sent to the MQTT broker.

---

### MainApplication (backend) project

- **`MqttMongoService`**  
  Connects to the MQTT broker, subscribes to `sensors/#` and processes incoming messages. Deserializes payloads into `SensorData`, inserts them into MongoDB, and optionally triggers the blockchain reward service.

- **`SensorData`**  
  MongoDB entity representing a single sensor reading. Contains `Id` (Mongo `_id`), `SensorId`, `Type`, `Value` and `Timestamp`.

- **`ISensorDataService`**  
  Abstraction for data access related to sensor readings, exposing methods to get data by type and produce dashboard data.

- **`SensorDataService`**  
  MongoDB-based implementation of `ISensorDataService`.  
  - Returns readings filtered by sensor type  
  - Builds dashboard entries per sensor (latest value, timestamp, and average of last N readings).

- **`SensorDataController`** (`[ApiController]`)  
  REST controller exposing endpoints for working with sensor data:  
  - `GET /api/SensorData/data` – returns filtered/sorted list of `SensorData`  
  - `GET /api/SensorData/export` – same data but with optional CSV export  
  - `GET /api/SensorData/dashboard` – returns aggregated `SensorDashboardDto` for dashboard views.

- **`SensorDashboardDto`**  
  DTO used for dashboard representation of a sensor: `SensorId`, `Type`, `LastValue`, `LastTimestamp` and `AverageLast100`.

---

### Blockchain integration

- **`IBlockchainRewardService`**  
  Interface defining two methods:
  - `RewardSensorAsync(sensorId)` – reward sensor’s wallet when a reading is received  
  - `GetBalancesAsync()` – return token balances for all configured sensors.

- **`BlockchainRewardService`**  
  Implementation using **Nethereum** and an ERC-20-like contract.  
  - Reads configuration from the `Blockchain` section (RPC URL, private key, contract address, reward size, sensor wallets)  
  - Supports **demo mode** when configuration is incomplete (no real transactions, only zero balances)  
  - Sends token transfers to sensor wallets upon reward request and reads current balances from the contract.

- **`SensorTokenAbi`**  
  Internal static class containing a minimal ERC-20 ABI as a JSON string, providing `transfer` and `balanceOf` definitions used by Nethereum.

- **`SensorTokenBalanceDto`**  
  DTO for token balances: `SensorId`, `WalletAddress` and `Balance` (in tokens, not wei). Used by both the API and the Token Balances Razor page.

- **`BlockchainController`** (`[ApiController]`)  
  Controller exposing blockchain-related endpoints:  
  - `GET /api/Blockchain/balances` – returns list of `SensorTokenBalanceDto` from `IBlockchainRewardService`.

---

### Razor Pages (MainApplication UI)

- **`Index`** (`Index.cshtml.cs`)  
  Simple page model for the landing page. Currently acts as an entry point that can be extended with links to other views.

- **`SensorChartModel`** (`SensorChart.cshtml.cs`)  
  Page model providing list of available sensor types (`light`, `temperature`, `air-quality`, `energy`) for chart visualizations.

- **`SensorDataModel`** (`SensorData.cshtml.cs`)  
  Page model for a table-style sensor data view. Uses `IHttpClientFactory` to call the backend `api/SensorData/data` endpoint and exposes the resulting `SensorDataList` and selected `FilterType` to the Razor view.

- **`TokenBalancesModel`** (`TokenBalances.cshtml.cs`)  
  Page model for token balances UI. Calls `api/Blockchain/balances` via a named `HttpClient`, deserializes JSON into `List<SensorTokenBalanceDto>` and exposes it to the Razor page.

---

### Startup / Hosting

- **`Program.cs` (DataGenerator)**  
  Configures DI (`MqttService`, `SensorSettingsProvider`, `SensorRunner`), connects to MQTT, starts publishing of virtual sensor data and exposes the `SensorsController` HTTP API on port 5001.

- **`Program.cs` (MainApplication)**  
  Configures MQTT and Mongo settings, registers `MqttMongoService`, `BlockchainRewardService` and `SensorDataService`, starts MQTT subscription and runs the ASP.NET Core app (Razor Pages + APIs) on port 5000.
