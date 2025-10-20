using MagicOnion;
using MagicOnion.Server;

namespace GameMap.Shared.Contracts;

public interface IMapHub : IService<IMapHub>
{
    // Queries
    UnaryResult<GetObjectsInAreaResponse> GetObjectsInArea(GetObjectsInAreaRequest request);
    UnaryResult<GetRegionsInAreaResponse> GetRegionsInArea(GetRegionsInAreaRequest request);

    // Object management
    UnaryResult<CreateObjectResponse> CreateObject(CreateObjectRequest request);
    UnaryResult<GetObjectResponse> GetObject(GetObjectRequest request);
    UnaryResult<RemoveObjectResponse> RemoveObject(RemoveObjectRequest request);
}
