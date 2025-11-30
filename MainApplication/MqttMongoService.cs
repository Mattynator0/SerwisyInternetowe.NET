using System.Buffers;

namespace MainApplication;

using MQTTnet;
using MongoDB.Driver;
using System.Text;
using System.Text.Json;


public class MqttMongoService
{
    private readonly IMqttClient _mqttClient;
    private readonly MqttClientOptions _options;

    public MqttMongoService(string mqttHost, int mqttPort, string mongoConnectionString, string mongoDbName, string mongoCollectionName)
    {
        var factory = new MqttClientFactory();
        _mqttClient = factory.CreateMqttClient();

        var client = new MongoClient(mongoConnectionString);
        var database = client.GetDatabase(mongoDbName);
        var collection = database.GetCollection<SensorData>(mongoCollectionName);

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
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());

                var data = JsonSerializer.Deserialize<SensorData>(payload);

                if (data != null)
                {
                    await collection.InsertOneAsync(data);
                    Console.WriteLine($"Saved sensor data for SensorId={data.SensorId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        };
    }
    
    public async Task StartAsync()
    {
        await _mqttClient.ConnectAsync(_options);
    }
}
