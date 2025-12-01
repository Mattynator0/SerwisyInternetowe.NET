using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MainApplication.Pages;

public class SensorDataModel(IHttpClientFactory httpClientFactory) : PageModel
{
    public List<string> AvailableTypes { get; set; } = ["light", "temperature", "air-quality", "energy"];
    public List<SensorData> SensorDataList { get; set; }
    public string FilterType { get; set; }

    public async Task OnGetAsync(string type)
    {
        Console.WriteLine(type);
        FilterType = type;

        var client = httpClientFactory.CreateClient("ApiClient");
        SensorDataList = await client.GetFromJsonAsync<List<SensorData>>($"api/SensorData/data?type={type}");
    }
}
