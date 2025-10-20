using GameMap.Network.Contracts;
using MagicOnion;

namespace GameMap.Network.Services;

/// <summary>
/// RPC contract for map queries.
/// </summary>
public interface IMapHub : IService<IMapHub>
{
    /// <summary>
    /// Returns objects intersecting the specified area.
    /// </summary>
    UnaryResult<GetObjectsInAreaResponse> GetObjectsInArea(GetObjectsInAreaRequest request);

    /// <summary>
    /// Returns regions intersecting the specified area.
    /// </summary>
    UnaryResult<GetRegionsInAreaResponse> GetRegionsInArea(GetRegionsInAreaRequest request);
}
