using GameMap.UdpClient.Models;
using Microsoft.AspNetCore.Mvc;

namespace GameMap.UdpClient.Controllers;

[ApiController]
[Route("api/client")]
public sealed class ClientController : ControllerBase
{
    private readonly UdpClientManager _mgr;

    public ClientController(UdpClientManager mgr) => _mgr = mgr;

    [HttpGet("status")]
    public ActionResult<UdpStatusDto> GetStatus()
        => Ok(_mgr.GetStatus());

    [HttpPost("connect")]
    public IActionResult Connect([FromBody] ConnectRequest req)
    {
        _mgr.Reconfigure(req.Host, req.Port, req.Key);
        return Accepted();
    }

    [HttpPost("messages/ping")]
    public async Task<IActionResult> Ping(CancellationToken ct)
    {
        var result = await _mgr.SendPingAsync(TimeSpan.FromSeconds(5), ct);
        return result.Success
            ? Ok(new PingResponseDto(result.ServerTicksUtcMs, result.RttMs))
            : Problem(result.Error ?? "Ping failed", statusCode: 504);
    }
}