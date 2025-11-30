namespace MainApplication;
using MongoDB.Bson;

public class SensorData
{
    public ObjectId Id { get; set; }
    public string SensorId { get; set; }
    public string Type { get; set; }
    public double Value { get; set; }
    public long Timestamp { get; set; }
}