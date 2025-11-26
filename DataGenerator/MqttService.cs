using MQTTnet;
using System.Text.Json;

namespace DataGenerator;

public class MqttService
{
    private readonly IMqttClient _client;

    public MqttService()
    {
        var factory = new MqttClientFactory();
        _client = factory.CreateMqttClient();
    }

    public async Task ConnectAsync(string brokerHost, int brokerPort)
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(brokerHost, brokerPort)
            .WithCleanSession()
            .Build();
        
        await _client.ConnectAsync(options);
    }
    
    public IMqttClient GetClient() => _client;
    
    public async Task PublishSensorDataAsync(string topic, object data)
    {
        var jsonData = JsonSerializer.Serialize(data);
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(jsonData)
            .Build();
        
        await _client.PublishAsync(message);
    }
}