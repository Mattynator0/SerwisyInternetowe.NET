using System;
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet;
using MongoDB.Driver;
using MainApplication.Blockchain;

namespace MainApplication;

public class MqttMongoService
{
    private const string SensorsTopicFilter = "sensors/#";
    
    private readonly IMqttClient _mqttClient;
    private readonly MqttClientOptions _mqttClientOptions;
    private readonly IMongoCollection<SensorData> _sensorDataCollection;
    private readonly IBlockchainRewardService? _blockchainRewardService;

    public MqttMongoService(
        string mqttHost,
        int mqttPort,
        string mongoConnectionString,
        string mongoDbName,
        string mongoCollectionName,
        IBlockchainRewardService? blockchainRewardService = null)
    {
        _mqttClient = CreateMqttClient();
        _mqttClientOptions = BuildMqttClientOptions(mqttHost, mqttPort);
        
        _sensorDataCollection = CreateMongoCollection(
            mongoConnectionString,
            mongoDbName,
            mongoCollectionName);
        
        _blockchainRewardService = blockchainRewardService;
        
        RegisterMqttEventHandlers();
    }

    private static IMqttClient CreateMqttClient()
    {
        var mqttClientFactory = new MqttClientFactory();
        return mqttClientFactory.CreateMqttClient();
    }
    
    private static MqttClientOptions BuildMqttClientOptions(string host, int port)
    {
        return new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithCleanSession()
            .Build();
    }
    
    private static IMongoCollection<SensorData> CreateMongoCollection(
        string connectionString,
        string databaseName,
        string collectionName)
    {
        var mongoClient = new MongoClient(connectionString);
        var database = mongoClient.GetDatabase(databaseName);
        return database.GetCollection<SensorData>(collectionName);
    }
    
    private void RegisterMqttEventHandlers()
    {
        _mqttClient.ConnectedAsync += OnMqttClientConnectedAsync;
        _mqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;
    }

    private async Task OnMqttClientConnectedAsync(MqttClientConnectedEventArgs _)
    {
        Console.WriteLine("Connected to MQTT broker");

        var subscribeOptions = BuildSensorsSubscriptionOptions();
        await _mqttClient.SubscribeAsync(subscribeOptions);

        Console.WriteLine($"Subscribed to {SensorsTopicFilter}");
    }

    private static MqttClientSubscribeOptions BuildSensorsSubscriptionOptions()
    {
        return new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(topicFilter =>
            {
                topicFilter.WithTopic(SensorsTopicFilter).WithAtMostOnceQoS();
            })
            .Build();
    }

    private async Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            var payloadJson = DecodePayloadToString(args.ApplicationMessage);
            var sensorData = DeserializeSensorData(payloadJson);

            if (sensorData is null)
            {
                return;
            }

            await SaveSensorDataAsync(sensorData);
            await RewardSensorIfConfiguredAsync(sensorData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling MQTT message: {ex.Message}");
        }
    }
    
    private static string DecodePayloadToString(MqttApplicationMessage applicationMessage)
    {
        var payloadBytes = applicationMessage.Payload;
        return Encoding.UTF8.GetString(payloadBytes.ToArray());
    }

    private static SensorData? DeserializeSensorData(string payloadJson)
    {
        return JsonSerializer.Deserialize<SensorData>(payloadJson);
    }
    
    private Task SaveSensorDataAsync(SensorData sensorData)
    {
        Console.WriteLine($"Saved sensor data for SensorId={sensorData.SensorId}");
        return _sensorDataCollection.InsertOneAsync(sensorData);
    }
    
    private Task RewardSensorIfConfiguredAsync(SensorData sensorData)
    {
        if (!CanRewardSensor(sensorData))
        {
            return Task.CompletedTask;
        }

        _ = _blockchainRewardService!.RewardSensorAsync(sensorData.SensorId);
        return Task.CompletedTask;
    }

    private bool CanRewardSensor(SensorData sensorData)
    {
        return _blockchainRewardService is not null
               && !string.IsNullOrEmpty(sensorData.SensorId);
    }
    
    public async Task StartAsync()
    {
        await _mqttClient.ConnectAsync(_mqttClientOptions);
    }
}
