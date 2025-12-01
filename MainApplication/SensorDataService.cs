namespace MainApplication;

using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ISensorDataService
{
    Task<List<SensorData>> GetByTypeAsync(string type);
    Task<List<SensorDashboardDto>> GetDashboardAsync();
}

public class SensorDataService : ISensorDataService
{
    private readonly IMongoCollection<SensorData> _collection;

    public SensorDataService(IMongoCollection<SensorData> collection)
    {
        _collection = collection;
    }

    public async Task<List<SensorData>> GetByTypeAsync(string type)
    {
        if (string.IsNullOrEmpty(type))
            return await _collection.Find(x => true).ToListAsync();
            
        var filter = Builders<SensorData>.Filter.Eq(s => s.Type, type);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<List<SensorDashboardDto>> GetDashboardAsync()
    {
        var result = new List<SensorDashboardDto>();

        var sensorIds = await _collection.Distinct<string>("SensorId", FilterDefinition<SensorData>.Empty).ToListAsync();

        foreach (var sensorId in sensorIds)
        {
            var filter = Builders<SensorData>.Filter.Eq(s => s.SensorId, sensorId);
            var sort = Builders<SensorData>.Sort.Descending(s => s.Timestamp);

            var last100 = await _collection
                .Find(filter)
                .Sort(sort)
                .Limit(100)
                .ToListAsync();

            if (!last100.Any())
                continue;

            var latest = last100.First();

            var dto = new SensorDashboardDto
            {
                SensorId = sensorId,
                Type = latest.Type,
                LastValue = latest.Value,
                LastTimestamp = latest.Timestamp,
                AverageLast100 = last100.Average(x => x.Value)
            };

            result.Add(dto);
        }

        return result;
    }
}
