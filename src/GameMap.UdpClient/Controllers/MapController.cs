using GameMap.SharedContracts.Networking.Packets;
using GameMap.UdpClient.Models;
using Microsoft.AspNetCore.Mvc;

namespace GameMap.UdpClient.Controllers;

[ApiController]
[Route("api/map")]
public sealed class MapController : ControllerBase
{
    private readonly UdpClientManager _mgr;

    public MapController(UdpClientManager mgr) => _mgr = mgr;

    [HttpPost("objects/query")]
    public async Task<ActionResult<GetObjectsInAreaResponse>> QueryObjects([FromBody] AreaQuery area, CancellationToken ct)
    {
        var result = await _mgr.RequestObjectsInAreaAsync(area.X1, area.Y1, area.X2, area.Y2, TimeSpan.FromSeconds(5), ct);
        return result.Success ? Ok(result.Response) : Problem(result.Error ?? "Objects query failed", statusCode: 504);
    }

    [HttpPost("regions/query")]
    public async Task<ActionResult<GetRegionsInAreaResponse>> QueryRegions([FromBody] AreaQuery area, CancellationToken ct)
    {
        var result = await _mgr.RequestRegionsInAreaAsync(area.X1, area.Y1, area.X2, area.Y2, TimeSpan.FromSeconds(5), ct);
        return result.Success ? Ok(result.Response) : Problem(result.Error ?? "Regions query failed", statusCode: 504);
    }

    [HttpPost("objects/add")]
    public async Task<ActionResult<AddObjectResponse>> AddObject([FromBody] GameObjectDto obj, CancellationToken ct)
    {
        var result = await _mgr.AddObjectAsync(obj, TimeSpan.FromSeconds(5), ct);
        return result.Success ? Ok(result.Response) : Problem(result.Error ?? "Add object failed", statusCode: 504);
    }
}