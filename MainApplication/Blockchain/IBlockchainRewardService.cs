using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MainApplication.Blockchain;

public interface IBlockchainRewardService
{
    Task RewardSensorAsync(string sensorId, CancellationToken ct = default);
    Task<IReadOnlyList<SensorTokenBalanceDto>> GetBalancesAsync(CancellationToken ct = default);
}
