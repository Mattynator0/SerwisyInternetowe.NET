using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MainApplication.Blockchain;
using Microsoft.AspNetCore.Mvc;

namespace MainApplication;

[ApiController]
[Route("api/[controller]")]
public class BlockchainController : ControllerBase
{
    private readonly IBlockchainRewardService _rewardService;

    public BlockchainController(IBlockchainRewardService rewardService)
    {
        _rewardService = rewardService;
    }

    // GET api/Blockchain/balances
    [HttpGet("balances")]
    public async Task<ActionResult<List<SensorTokenBalanceDto>>> GetBalances(CancellationToken ct)
    {
        var balances = await _rewardService.GetBalancesAsync(ct);
        return Ok(balances);
    }
}
