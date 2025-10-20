using System.Threading.Tasks;
using GameMap.Shared.Contracts;
using Grpc.Net.Client;
using MagicOnion.Client;

namespace GameMap.Client.Scripts
{
    public static class SampleQuery
    {
        public static async Task RunAsync()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            using var channel = GrpcChannel.ForAddress("http://localhost:5000");
            var client = MagicOnionClient.Create<IMapHub>(channel);
            var area = new GetObjectsInAreaRequest { X1 = 0, Y1 = 0, X2 = 20, Y2 = 20 };
            var resp = await client.GetObjectsInArea(area);
            Console.WriteLine($"Objects in area: {resp.Objects.Length}");
        }
    }
}
