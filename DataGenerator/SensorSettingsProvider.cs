namespace DataGenerator;

public interface ISensorSettingsProvider
{
    IReadOnlyDictionary<string, SensorGenerationSettings> GetAllSensorsSettings();
}

public class SensorSettingsProvider : ISensorSettingsProvider
{
    private static readonly IReadOnlyDictionary<string, SensorGenerationSettings> DefaultSensorTypeSettings =
        new Dictionary<string, SensorGenerationSettings>(StringComparer.OrdinalIgnoreCase)
        {
            ["light"]       = new SensorGenerationSettings(0.0,  1000.0, 20.0),
            ["air-quality"] = new SensorGenerationSettings(0.0,   500.0, 15.0),
            ["temperature"] = new SensorGenerationSettings(-10.0,  50.0, 30.0),
            ["energy"]      = new SensorGenerationSettings(0.0,    10.0, 10.0)
        };
    
    private readonly IConfiguration _configuration;

    public SensorSettingsProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IReadOnlyDictionary<string, SensorGenerationSettings> GetAllSensorsSettings()
    {
        var sensorSettingsByType = new Dictionary<string, SensorGenerationSettings>(StringComparer.OrdinalIgnoreCase);

        foreach (var (sensorType, _) in DefaultSensorTypeSettings)
        {
            sensorSettingsByType[sensorType] = 
                BuildSensorGenerationSettingsForType(sensorType);
        }

        return sensorSettingsByType;
    }
    
    private SensorGenerationSettings BuildSensorGenerationSettingsForType(string sensorType)
    {
        var section = _configuration.GetSection($"Sensors:{sensorType}");
        SensorGenerationSettings defaultSettings = DefaultSensorTypeSettings[sensorType];
        return ReadConfigSettingOrDefault(section, defaultSettings);
    }

    private static SensorGenerationSettings ReadConfigSettingOrDefault(
        IConfigurationSection section,
        SensorGenerationSettings defaultSensorTypeSettings)
    {
        return new SensorGenerationSettings(
            section.GetValue<double?>("Min") ?? defaultSensorTypeSettings.Min,
            section.GetValue<double?>("Max") ?? defaultSensorTypeSettings.Max,
            section.GetValue<double?>("MessagesPerMinute") 
                               ?? defaultSensorTypeSettings.MessagesPerMinute);
    }
}