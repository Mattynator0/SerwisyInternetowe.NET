using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace MainApplication.Blockchain;

public class BlockchainRewardService : IBlockchainRewardService
{
    private readonly bool _enabled;
    private readonly Web3? _web3;
    private readonly string _contractAddress = string.Empty;
    private readonly string _ownerAddress = string.Empty;
    private readonly BigInteger _rewardPerMessageWei;
    private readonly IReadOnlyDictionary<string, string> _sensorWallets;

    public BlockchainRewardService(IConfiguration configuration)
    {
        var section = configuration.GetSection("Blockchain");

        // ZAWSZE wczytujemy listę sensorów - nawet jeśli blockchain jest wyłączony
        var sensorsSection = section.GetSection("Sensors");
        _sensorWallets = sensorsSection.GetChildren()
            .Where(c => !string.IsNullOrWhiteSpace(c.Value))
            .ToDictionary(c => c.Key, c => c.Value!, StringComparer.OrdinalIgnoreCase);

        var rpcUrl = section["RpcUrl"];
        var privateKey = section["OwnerPrivateKey"];
        var contractAddr = section["ContractAddress"];

        if (string.IsNullOrWhiteSpace(rpcUrl) ||
            string.IsNullOrWhiteSpace(privateKey) ||
            string.IsNullOrWhiteSpace(contractAddr))
        {
            // brak pełnej konfiguracji - pracujemy w trybie DEMO (zero tokenów, ale wiersze są widoczne)
            _enabled = false;
            _rewardPerMessageWei = BigInteger.Zero;
            Console.WriteLine("[Blockchain] Module disabled – missing RpcUrl / OwnerPrivateKey / ContractAddress. Running in UI-only mode.");
            return;
        }

        _enabled = true;
        _contractAddress = contractAddr;

        var rewardTokens = section.GetValue<long?>("RewardPerMessage") ?? 1L;
        _rewardPerMessageWei = Web3.Convert.ToWei(rewardTokens);

        var account = new Account(privateKey);
        _ownerAddress = account.Address;
        _web3 = new Web3(account, rpcUrl);

        Console.WriteLine($"[Blockchain] Module enabled. Contract={_contractAddress}, owner={_ownerAddress}, reward={rewardTokens} tokens/msg");
    }

    public async Task RewardSensorAsync(string sensorId, CancellationToken ct = default)
    {
        if (!_enabled || _web3 == null)
            return;

        if (!_sensorWallets.TryGetValue(sensorId, out var wallet) ||
            string.IsNullOrWhiteSpace(wallet))
        {
            // brak zdefiniowanego portfela dla tego sensora - po prostu nic nie robimy
            return;
        }

        try
        {
            var contract = _web3.Eth.GetContract(SensorTokenAbi.Value, _contractAddress);
            var transferFunction = contract.GetFunction("transfer");

            HexBigInteger gasWithBuffer;

            // 1. Spróbuj oszacować gas
            try
            {
                var estimatedGas = await transferFunction.EstimateGasAsync(
                    from: _ownerAddress,
                    gas: null,
                    value: null,
                    functionInput: new object[] { wallet, _rewardPerMessageWei });

                gasWithBuffer = new HexBigInteger(estimatedGas.Value + 5000);
            }
            catch (Exception ex)
            {
                // Jeśli node nie pozwala na eth_estimateGas - używamy stałej wartości
                Console.WriteLine($"[Blockchain] Gas estimation failed for {sensorId}: {ex.Message}. Using default gas 100000.");
                gasWithBuffer = new HexBigInteger(100_000);
            }

            // 2. Wyślij transakcję z limitem gazu
            var txHash = await transferFunction.SendTransactionAsync(
                from: _ownerAddress,
                gas: gasWithBuffer,
                value: null,
                functionInput: new object[] { wallet, _rewardPerMessageWei });

            Console.WriteLine($"[Blockchain] Rewarded sensor {sensorId} ({wallet}) with {_rewardPerMessageWei} wei tokens. Tx={txHash}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Blockchain] Error rewarding sensor {sensorId}: {ex.Message}");
        }
    }



    public async Task<IReadOnlyList<SensorTokenBalanceDto>> GetBalancesAsync(CancellationToken ct = default)
    {
        // Jeśli nie ma żadnych sensorów w konfiguracji - zwróć pustą listę
        if (_sensorWallets.Count == 0)
            return new List<SensorTokenBalanceDto>();

        // TRYB DEMO: blockchain wyłączony - zwracamy wiersze z zerowym stanem
        if (!_enabled || _web3 == null)
        {
            return _sensorWallets
                .Select(kvp => new SensorTokenBalanceDto
                {
                    SensorId = kvp.Key,
                    WalletAddress = kvp.Value,
                    Balance = 0m
                })
                .ToList();
        }

        var result = new List<SensorTokenBalanceDto>();

        try
        {
            var contract = _web3.Eth.GetContract(SensorTokenAbi.Value, _contractAddress);
            var balanceOfFunction = contract.GetFunction("balanceOf");

            foreach (var (sensorId, wallet) in _sensorWallets)
            {
                if (string.IsNullOrWhiteSpace(wallet))
                    continue;

                try
                {
                    var balanceWei = await balanceOfFunction.CallAsync<System.Numerics.BigInteger>(wallet);
                    var balanceTokens = Web3.Convert.FromWei(balanceWei);

                    result.Add(new SensorTokenBalanceDto
                    {
                        SensorId = sensorId,
                        WalletAddress = wallet,
                        Balance = balanceTokens
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Blockchain] Error reading balance for {sensorId}: {ex.Message}");

                    // fallback: pokaż wiersz, ale z balansem 0
                    result.Add(new SensorTokenBalanceDto
                    {
                        SensorId = sensorId,
                        WalletAddress = wallet,
                        Balance = 0m
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Blockchain] Error initializing contract for balances: {ex.Message}");

            // totalny fallback: nawet kontrakt się nie utworzył - lista sensorów z balansem 0
            return _sensorWallets
                .Select(kvp => new SensorTokenBalanceDto
                {
                    SensorId = kvp.Key,
                    WalletAddress = kvp.Value,
                    Balance = 0m
                })
                .ToList();
        }

        return result;
    }


}
