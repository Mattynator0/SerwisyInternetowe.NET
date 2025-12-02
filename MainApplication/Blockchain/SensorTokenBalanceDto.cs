namespace MainApplication.Blockchain;
public class SensorTokenBalanceDto
{
    public string SensorId { get; set; } = string.Empty;
    public string WalletAddress { get; set; } = string.Empty;
    public decimal Balance { get; set; } // w STKN, nie w wei
}
