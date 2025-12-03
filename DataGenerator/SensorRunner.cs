namespace DataGenerator;

public class SensorRunner
{
    private const int SensorsPerType = 4;
    
    private readonly MqttService _mqttService;
    private readonly ISensorSettingsProvider _settingsProvider;
    private readonly List<Sensor> _sensors = new();

    public SensorRunner(MqttService mqttService, ISensorSettingsProvider settingsProvider)
    {
        _mqttService = mqttService;
        _settingsProvider = settingsProvider;
    }

    public void InitializeSensors()
    {
        var sensorGenerationSettingsByType 
            = _settingsProvider.GetAllSensorsSettings();

        _sensors.Clear();

        foreach (var (sensorType, settings) in sensorGenerationSettingsByType)
        {
            CreateSensorsForType(sensorType, settings);
        }
    }
    
    private void CreateSensorsForType(string sensorType, SensorGenerationSettings settings)
    {
        for (int index = 1; index <= SensorsPerType; index++)
        {
            var sensor = CreateSensor(sensorType, index, settings);
            _sensors.Add(sensor);
        }
    }

    private Sensor CreateSensor(string sensorType, int index, SensorGenerationSettings settings)
    {
        string sensorId = $"{sensorType}{index}";

        return new Sensor(
            sensorId: sensorId,
            sensorType: sensorType,
            mqttService: _mqttService,
            min: settings.Min,
            max: settings.Max,
            measurementPublishIntervalMs: settings.MeasurementPublishIntervalMs);
    }

    public void StartPublishing()
    {
        foreach (var sensor in _sensors)
        {
            _ = RunSensorPublishingLoopAsync(sensor);
        }
    }

    private static async Task RunSensorPublishingLoopAsync(Sensor sensor)
    {
        while (true)
        {
            await sensor.PublishMeasurementDataAsync();
            await Task.Delay(sensor.MeasurementPublishIntervalMs);
        }
    }
}