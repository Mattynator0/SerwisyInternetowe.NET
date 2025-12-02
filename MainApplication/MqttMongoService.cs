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
    private readonly IMqttClient _mqttClient;
    private readonly MqttClientOptions _options;
    private readonly IMongoCollection<SensorData> _collection;
    private readonly IBlockchainRewardService? _blockchainRewardService;

    public MqttMongoService(
        string mqttHost,
        int mqttPort,
        string mongoConnectionString,
        string mongoDbName,
        string mongoCollectionName,
        IBlockchainRewardService? blockchainRewardService = null)
    {
        var factory = new MqttClientFactory();
        _mqttClient = factory.CreateMqttClient();

        var client = new MongoClient(mongoConnectionString);
        var database = client.GetDatabase(mongoDbName);
        _collection = database.GetCollection<SensorData>(mongoCollectionName);

        _blockchainRewardService = blockchainRewardService;

        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttHost, mqttPort)
            .WithCleanSession()
            .Build();

        _mqttClient.ConnectedAsync += async e =>
        {
            Console.WriteLine("Connected to MQTT broker");
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f =>
                {
                    f.WithTopic("sensors/#")
                     .WithAtMostOnceQoS();
                })
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions);

            Console.WriteLine("Subscribed to sensors/#");
        };

        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            try
            {
                // Payload jest typu ReadOnlySequence<byte> – używamy ToArray()
                var payloadBytes = e.ApplicationMessage.Payload;
                var payload = Encoding.UTF8.GetString(payloadBytes.ToArray());

                var data = JsonSerializer.Deserialize<SensorData>(payload);

                if (data != null)
                {
                    await _collection.InsertOneAsync(data);
                    Console.WriteLine($"Saved sensor data for SensorId={data.SensorId}");

                    // nagroda w tokenach (fire-and-forget)
                    if (_blockchainRewardService != null &&
                        !string.IsNullOrEmpty(data.SensorId))
                    {
                        _ = _blockchainRewardService.RewardSensorAsync(data.SensorId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling MQTT message: {ex.Message}");
            }
        };
    }

    public async Task StartAsync()
    {
        await _mqttClient.ConnectAsync(_options);
    }
}
