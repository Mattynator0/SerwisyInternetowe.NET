using System.Text.Json;
using MainApplication.Blockchain;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MainApplication.Pages;

public class TokenBalancesModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public List<SensorTokenBalanceDto> Balances { get; set; } = new();

    public TokenBalancesModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task OnGetAsync()
    {
        var client = _httpClientFactory.CreateClient("ApiClient");
        var response = await client.GetAsync("api/Blockchain/balances");

        if (!response.IsSuccessStatusCode)
            return;

        var json = await response.Content.ReadAsStringAsync();
        Balances = JsonSerializer.Deserialize<List<SensorTokenBalanceDto>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
    }
}
