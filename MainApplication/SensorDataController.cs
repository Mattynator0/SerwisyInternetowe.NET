using System.Globalization;
using System.Text;

namespace MainApplication;

using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class SensorDataController(ISensorDataService service) : ControllerBase
{
    [HttpGet("data")]
    public async Task<IActionResult> GetTableData(
        [FromQuery] string type,
        [FromQuery] string sortColumn = "Timestamp",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] string filterSensorId = null,
        [FromQuery] string filterTimestampBefore = null,
        [FromQuery] string filterTimestampAfter = null)
    {
        var data = await GetData(type, sortColumn, sortDirection, filterSensorId, filterTimestampBefore, filterTimestampAfter);

        return Ok(data);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportData(
        [FromQuery] string type,
        [FromQuery] string sortColumn = "timestamp",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] string format = "csv",
        [FromQuery] string filterSensorId = null,
        [FromQuery] string filterTimestampBefore = null,
        [FromQuery] string filterTimestampAfter = null)
    {
        var data = await GetData(type, sortColumn, sortDirection, filterSensorId, filterTimestampBefore, filterTimestampAfter);

        if (!format.Equals("csv"))
            return Ok(data);

        var csvBuilder = new StringBuilder();
        using var writer = new StringWriter(csvBuilder);
        using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(data);
        return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", "data.csv");
    }

    private async Task<List<SensorData>> GetData(
        string type,
        string sortColumn,
        string sortDirection,
        string filterSensorId,
        string filterTimestampBefore,
        string filterTimestampAfter)
    {
        var data = await service.GetByTypeAsync(type);
        
        if (!string.IsNullOrEmpty(filterSensorId))
            data = data.Where(d => d.SensorId.Contains(filterSensorId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrEmpty(filterTimestampBefore) && long.TryParse(filterTimestampBefore, out var dtb))
            data = data.Where(d => d.Timestamp < dtb).ToList();
        if (!string.IsNullOrEmpty(filterTimestampAfter) && long.TryParse(filterTimestampAfter, out var dta))
            data = data.Where(d => d.Timestamp > dta).ToList();
        
        Func<SensorData, object> keySelector = sortColumn.ToLower() switch
        {
            "sensorid" => x => x.SensorId,
            "type" => x => x.Type,
            "value" => x => x.Value,
            _ => x => x.Timestamp
        };
        data = sortDirection.Equals("desc")
            ? data.OrderByDescending(keySelector).ToList()
            : data.OrderBy(keySelector).ToList();
        return data;
    }
}