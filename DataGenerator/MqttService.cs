using MQTTnet;
using System.Text.Json;

namespace DataGenerator;

public class MqttService
{
    private readonly IMqttClient _client;

    public MqttService()
    {
        var mqttClientFactory = new MqttClientFactory();
        _client = mqttClientFactory.CreateMqttClient();
    }

    public async Task ConnectToMqttBrokerAsync(string brokerHost, int brokerPort)
    {
        MqttClientOptions options = BuildMqttClientOptions(brokerHost, brokerPort);
        await _client.ConnectAsync(options);
    }
    
    private static MqttClientOptions BuildMqttClientOptions(
        string brokerHost, int brokerPort)
    {
        return new MqttClientOptionsBuilder()
            .WithTcpServer(brokerHost, brokerPort)
            .WithCleanSession()
            .Build();
    }
    
    public async Task PublishSensorDataAsync(string mqttTopic, object data)
    {
        var jsonData = JsonSerializer.Serialize(data);
        var message = BuildMqttApplicationMessage(mqttTopic, jsonData);
        await _client.PublishAsync(message);
    }

    private static MqttApplicationMessage BuildMqttApplicationMessage(string mqttTopic, string jsonData)
    {
        return new MqttApplicationMessageBuilder()
            .WithTopic(mqttTopic)
            .WithPayload(jsonData)
            .Build();
    }
}