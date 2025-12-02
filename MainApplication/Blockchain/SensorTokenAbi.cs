namespace MainApplication.Blockchain;

/// <summary>
/// Minimalne ABI ERC-20 z funkcjami transfer i balanceOf.
/// Jeśli Twój kontrakt ma inne funkcje/nazwy – podmień ABI na swoje.
/// </summary>
internal static class SensorTokenAbi
{
    public const string Value = @"[
  {
    ""constant"": false,
    ""inputs"": [
      { ""name"": ""_to"", ""type"": ""address"" },
      { ""name"": ""_value"", ""type"": ""uint256"" }
    ],
    ""name"": ""transfer"",
    ""outputs"": [
      { ""name"": """", ""type"": ""bool"" }
    ],
    ""type"": ""function""
  },
  {
    ""constant"": true,
    ""inputs"": [
      { ""name"": ""_owner"", ""type"": ""address"" }
    ],
    ""name"": ""balanceOf"",
    ""outputs"": [
      { ""name"": ""balance"", ""type"": ""uint256"" }
    ],
    ""type"": ""function""
  }
]";
}
