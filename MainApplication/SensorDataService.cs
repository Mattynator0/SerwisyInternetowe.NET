namespace MainApplication;

using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ISensorDataService
{
    Task<List<SensorData>> GetBySensorTypeAsync(string sensorType);
    Task<List<SensorDashboardDto>> GetDashboardAsync();
}

public class SensorDataService : ISensorDataService
{
    private const int DashboardReadingsSampleSize = 100;
    private readonly IMongoCollection<SensorData> _sensorDataCollection;

    public SensorDataService(IMongoCollection<SensorData> sensorDataCollection)
    {
        _sensorDataCollection = sensorDataCollection;
    }

    public Task<List<SensorData>> GetBySensorTypeAsync(string sensorType)
    {
        var filter = BuildSensorTypeFilter(sensorType);
        return _sensorDataCollection.Find(filter).ToListAsync();
    }

    private static FilterDefinition<SensorData> BuildSensorTypeFilter(string sensorType)
    {
        return string.IsNullOrWhiteSpace(sensorType)
            ? Builders<SensorData>.Filter.Empty
            : Builders<SensorData>.Filter.Eq(s => s.Type, sensorType);
    }
    
    public async Task<List<SensorDashboardDto>> GetDashboardAsync()
    {
        var distinctSensorIds = await GetDistinctSensorIdsAsync();
        return await BuildDashboardEntriesAsync(distinctSensorIds);
    }
    
    private Task<List<string>> GetDistinctSensorIdsAsync()
    {
        return _sensorDataCollection
            .Distinct<string>(nameof(SensorData.SensorId), FilterDefinition<SensorData>.Empty)
            .ToListAsync();
    }
    
    private async Task<List<SensorDashboardDto>> BuildDashboardEntriesAsync(IEnumerable<string> sensorIds)
    {
        var dashboardEntries = new List<SensorDashboardDto>();

        foreach (var sensorId in sensorIds)
        {
            var lastReadings = 
                await GetNLastReadingsForSensorAsync(sensorId, DashboardReadingsSampleSize);
            if (lastReadings.Count == 0)
                continue;
            

            var entry = CreateDashboardEntry(sensorId, lastReadings);
            dashboardEntries.Add(entry);
        }

        return dashboardEntries;
    }
    
    private Task<List<SensorData>> GetNLastReadingsForSensorAsync(string sensorId, int limit)
    {
        var filter = Builders<SensorData>.Filter.Eq(s => s.SensorId, sensorId);
        var sort = Builders<SensorData>.Sort.Descending(s => s.Timestamp);

        return _sensorDataCollection
            .Find(filter)
            .Sort(sort)
            .Limit(limit)
            .ToListAsync();
    }

    private static SensorDashboardDto CreateDashboardEntry(string sensorId, IReadOnlyList<SensorData> readings)
    {
        var latest = readings[0];
        var averageValue = readings.Average(r => r.Value);

        return new SensorDashboardDto
        {
            SensorId = sensorId,
            Type = latest.Type,
            LastValue = latest.Value,
            LastTimestamp = latest.Timestamp,
            AverageLast100 = averageValue
        };
    }
}
