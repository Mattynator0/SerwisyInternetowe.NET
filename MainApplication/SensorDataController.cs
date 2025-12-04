using System.Globalization;
using System.Text;

namespace MainApplication;

using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class SensorDataController : ControllerBase
{
    private readonly ISensorDataService _sensorDataService;

    public SensorDataController(ISensorDataService sensorDataService)
    {
        _sensorDataService = sensorDataService;
    }
    [HttpGet("data")]
    public async Task<IActionResult> GetTableDataAsync(
        [FromQuery] string type,
        [FromQuery] string sortColumn = "Timestamp",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] string filterSensorId = null,
        [FromQuery] string filterTimestampBefore = null,
        [FromQuery] string filterTimestampAfter = null)
    {
        var data = await GetFilteredAndSortedDataAsync(
            type, sortColumn, sortDirection, filterSensorId, filterTimestampBefore, filterTimestampAfter);

        return Ok(data);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportSensorDataAsync(
        [FromQuery] string type,
        [FromQuery] string sortColumn = "timestamp",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] string format = "csv",
        [FromQuery] string filterSensorId = null,
        [FromQuery] string filterTimestampBefore = null,
        [FromQuery] string filterTimestampAfter = null)
    {
        var data = await GetFilteredAndSortedDataAsync(type, sortColumn, sortDirection, filterSensorId, filterTimestampBefore, filterTimestampAfter);

        if (!format.Equals("csv"))
            return Ok(data);

        return CreateCsvFile(data);
    }

    private FileContentResult CreateCsvFile(IEnumerable<SensorData> sensorData)
    {
        var csvBuilder = new StringBuilder();
        using var writer = new StringWriter(csvBuilder);
        using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(sensorData);
        return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", "data.csv");
    }

    private async Task<List<SensorData>> GetFilteredAndSortedDataAsync(
        string sensorType,
        string sortColumn,
        string sortDirection,
        string filterSensorId,
        string filterTimestampBefore,
        string filterTimestampAfter)
    {
        List<SensorData> sensorData = await _sensorDataService.GetBySensorTypeAsync(sensorType);

        IEnumerable<SensorData> filtered = ApplyFilters(
            sensorData, filterSensorId, filterTimestampBefore, filterTimestampAfter);
        IEnumerable<SensorData> sorted = ApplySorting(
            filtered, sortColumn, sortDirection);

        return sorted.ToList();
    }
    
    private static IEnumerable<SensorData> ApplyFilters(
        IEnumerable<SensorData> data,
        string? filterSensorId,
        string? filterTimestampBefore,
        string? filterTimestampAfter)
    {
        var query = data;

        query = ApplySensorIdFilter(query, filterSensorId);
        query = ApplyTimestampBeforeFilter(query, filterTimestampBefore);
        query = ApplyTimestampAfterFilter(query, filterTimestampAfter);

        return query;
    }

    private static IEnumerable<SensorData> ApplySensorIdFilter(
        IEnumerable<SensorData> data, string? filterSensorId)
    {
        if (string.IsNullOrWhiteSpace(filterSensorId))
            return data;
        
        return data.Where(d =>
            d.SensorId.Contains(filterSensorId, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<SensorData> ApplyTimestampBeforeFilter(
        IEnumerable<SensorData> data,
        string? filterTimestampBefore)
    {
        long? timestampBefore = ParseTimestampFilterOrNull(filterTimestampBefore);
        if (timestampBefore is null)
            return data;
        
        return data.Where(d => d.Timestamp < timestampBefore.Value);
    }
    
    private static long? ParseTimestampFilterOrNull(string? timestampFilter)
    {
        if (string.IsNullOrWhiteSpace(timestampFilter))
            return null;
        
        return long.TryParse(timestampFilter, out var parsed) ? parsed : null;
    }

    private static IEnumerable<SensorData> ApplyTimestampAfterFilter(
        IEnumerable<SensorData> data,
        string? filterTimestampAfter)
    {
        long? timestampAfter = ParseTimestampFilterOrNull(filterTimestampAfter);
        if (timestampAfter is null)
            return data;
        
        return data.Where(d => d.Timestamp > timestampAfter.Value);
    }
    
    private static IEnumerable<SensorData> ApplySorting(
        IEnumerable<SensorData> data,
        string sortColumn,
        string sortDirection)
    {
        var keySelector = CreateSortKeySelector(sortColumn);
        bool isDescending = IsSortDirectionDescending(sortDirection);

        return SortSensorData(data, keySelector, isDescending);
    }

    private static Func<SensorData, object> CreateSortKeySelector(string? sortColumn)
    {
        return (sortColumn ?? string.Empty).ToLowerInvariant() switch
        {
            "sensorid" => sensor => sensor.SensorId,
            "type"     => sensor => sensor.Type,
            "value"    => sensor => sensor.Value,
            _          => sensor => sensor.Timestamp
        };
    }

    private static bool IsSortDirectionDescending(string? sortDirection)
    {
        return sortDirection != null &&
               sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<SensorData> SortSensorData(
        IEnumerable<SensorData> data,
        Func<SensorData, object> keySelector,
        bool isDescending)
    {
        return isDescending
            ? data.OrderByDescending(keySelector)
            : data.OrderBy(keySelector);
    }
    
    [HttpGet("dashboard")]
    public async Task<ActionResult<List<SensorDashboardDto>>> GetDashboard()
    {
        var data = await _sensorDataService.GetDashboardAsync();
        return Ok(data);
    }
}