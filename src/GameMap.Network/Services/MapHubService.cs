using System.Linq;
using GameMap.Shared.Contracts;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Server;

namespace GameMap.Network.Services;

public sealed class MapHubService : ServiceBase<IMapHub>, IMapHub
{
    private readonly IObjectLayer _objects;
    private readonly IRegionLayer _regions;

    public MapHubService(IObjectLayer objects,IRegionLayer regions)
    {
        _objects = objects;
        _regions = regions;
    }

    public UnaryResult<GetObjectsInAreaResponse> GetObjectsInArea(GetObjectsInAreaRequest request)
    {
        try
        {
            if (request.X1 > request.X2 || request.Y1 > request.Y2)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid rectangle: (x1,y1) must be <= (x2,y2)."));

            var list = _objects.GetObjectsInArea(request.X1, request.Y1, request.X2, request.Y2)
                .Select(o => new ObjectDto { Id = o.Id, X = o.X, Y = o.Y, Width = o.Width, Height = o.Height })
                .ToArray();

            return UnaryResult.FromResult(new GetObjectsInAreaResponse { Objects = list });
        }
        catch (RpcException)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Internal, "Failed to query objects."));
        }
    }

    public UnaryResult<GetRegionsInAreaResponse> GetRegionsInArea(GetRegionsInAreaRequest request)
    {
        try
        {
            return UnaryResult.FromResult(new GetRegionsInAreaResponse { Regions = [] });
        }
        catch (RpcException)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Internal, "Failed to query regions."));
        }
    }

    public UnaryResult<CreateObjectResponse> CreateObject(CreateObjectRequest request)
    {
        try
        {
            var obj = new MapObject(request.Id, request.X, request.Y, request.Width, request.Height);
            if (!_objects.CanPlaceObject(obj))
            {
                return UnaryResult.FromResult(new CreateObjectResponse { Success = false, Error = "Cannot place object on this terrain." });
            }

            _objects.AddObject(obj);
            var dto = new ObjectDto { Id = obj.Id, X = obj.X, Y = obj.Y, Width = obj.Width, Height = obj.Height };
            return UnaryResult.FromResult(new CreateObjectResponse { Success = true, Object = dto });
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public UnaryResult<GetObjectResponse> GetObject(GetObjectRequest request)
    {
        var obj = _objects.GetObject(request.Id);
        var dto = obj == null ? null : new ObjectDto { Id = obj.Id, X = obj.X, Y = obj.Y, Width = obj.Width, Height = obj.Height };
        return UnaryResult.FromResult(new GetObjectResponse { Object = dto });
    }

    public UnaryResult<RemoveObjectResponse> RemoveObject(RemoveObjectRequest request)
    {
        _objects.RemoveObject(request.Id);
        return UnaryResult.FromResult(new RemoveObjectResponse { Success = true });
    }
}
