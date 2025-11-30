namespace MainApplication;

using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ISensorDataService
{
    Task<List<SensorData>> GetByTypeAsync(string type);
}

public class SensorDataService(IMongoCollection<SensorData> collection) : ISensorDataService
{
    public async Task<List<SensorData>> GetByTypeAsync(string type)
    {
        if (string.IsNullOrEmpty(type))
            return await collection.Find(x => true).ToListAsync();
            
        var filter = Builders<SensorData>.Filter.Eq(s => s.Type, type);
        return await collection.Find(filter).ToListAsync();
    }
}
