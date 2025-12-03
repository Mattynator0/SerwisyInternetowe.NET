using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MainApplication.Pages;

public class SensorChartModel : PageModel
{
    public List<string> AvailableTypes { get; set; } = ["light", "temperature", "air-quality", "energy"];
}