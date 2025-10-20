using System.Linq;
using GameMap.Network.Contracts;
using MagicOnion;
using MagicOnion.Server;

namespace GameMap.Network.Services;

public sealed class MapHubService : ServiceBase<IMapHub>, IMapHub
{
    private readonly global::GameMap.Core.Layers.IObjectLayer _objects;

    public MapHubService(global::GameMap.Core.Layers.IObjectLayer objects)
    {
        _objects = objects;
    }

    public async UnaryResult<GetObjectsInAreaResponse> GetObjectsInArea(GetObjectsInAreaRequest request)
    {
        var list = _objects.GetObjectsInArea(request.X1, request.Y1, request.X2, request.Y2)
            .Select(o => new ObjectDto(o.Id, o.X, o.Y, o.Width, o.Height))
            .ToArray();
        return await UnaryResult.FromResult(new GetObjectsInAreaResponse(list));
    }

    public async UnaryResult<GetRegionsInAreaResponse> GetRegionsInArea(GetRegionsInAreaRequest request)
    {
        // Regions not implemented yet
        return await UnaryResult.FromResult(new GetRegionsInAreaResponse(Array.Empty<RegionDto>()));
    }
}
