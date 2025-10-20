using System.Threading.Tasks;
using GameMap.Shared.Contracts;
using Grpc.Net.Client;
using MagicOnion.Client;
using GameMap.Client.Udp;

internal class Program
{
    private static async Task Main(string[] args)
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        // Start gRPC/MagicOnion client
        using var channel = GrpcChannel.ForAddress("http://localhost:5000");
        var hub = MagicOnionClient.Create<IMapHub>(channel);

        // Clean up any leftovers from previous runs and seeded demo
        await SafeRemove(hub, "obj-1");
        await SafeRemove(hub, "obj-2");
        await SafeRemove(hub, "house_1");
        await SafeRemove(hub, "big_building");
        await SafeRemove(hub, "small_house");

        // Scenario 1: valid placement inside bounds
        var create1 = await hub.CreateObject(new CreateObjectRequest { Id = "house_1", X = 5, Y = 5, Width = 3, Height = 3 });
        Console.WriteLine(create1.Success
            ? "[gRPC] Scenario 1: house_1 placed"
            : $"[gRPC] Scenario 1: failed to place house_1: {create1.Error}");

        // Scenario 2: out-of-bounds (expect failure)
        var create2 = await hub.CreateObject(new CreateObjectRequest { Id = "big_building", X = 38, Y = 18, Width = 5, Height = 5 });
        Console.WriteLine(create2.Success
            ? "[gRPC] Scenario 2: big_building placed (unexpected)"
            : $"[gRPC] Scenario 2: big_building rejected: {create2.Error}");

        // Scenario 3: overlapping object (expect failure)
        var create3 = await hub.CreateObject(new CreateObjectRequest { Id = "small_house", X = 6, Y = 6, Width = 2, Height = 2 });
        Console.WriteLine(create3.Success
            ? "[gRPC] Scenario 3: small_house placed (unexpected)"
            : $"[gRPC] Scenario 3: small_house rejected: {create3.Error}");

        // UDP/LiteNetLib + MemoryPack queries
        //var timeout = TimeSpan.FromSeconds(3);
        //var area = new GetObjectsInAreaRequest { X1 = 0, Y1 = 0, X2 = 20, Y2 = 20 };
        //var udpObjs = await UdpAreaClient.QueryObjectsAsync("127.0.0.1", 9050, area, timeout);
        //Console.WriteLine($"[UDP] Objects in area (0,0)-(20,20): {udpObjs.Objects.Length}");
        //foreach (var o in udpObjs.Objects)
        //    Console.WriteLine($"  - {o.Id}: ({o.X},{o.Y}) {o.Width}x{o.Height}");

        //var regReq = new GetRegionsInAreaRequest { X1 = 0, Y1 = 0, X2 = 20, Y2 = 20 };
        //var udpRegs = await UdpAreaClient.QueryRegionsAsync("127.0.0.1", 9050, regReq, timeout);
        //Console.WriteLine($"[UDP] Regions in area (0,0)-(20,20): {udpRegs.Regions.Length}");

        //static uint? FindRegionIdFor(int x, int y, GetRegionsInAreaResponse resp)
        //{
        //    foreach (var r in resp.Regions)
        //        if (x >= r.X1 && x <= r.X2 && y >= r.Y1 && y <= r.Y2)
        //            return r.Id;
        //    return null;
        //}

        //var r1 = FindRegionIdFor(2, 2, udpRegs);
        //var r2 = FindRegionIdFor(9, 9, udpRegs);
        //Console.WriteLine($"Tile (2,2) region: {r1?.ToString() ?? "n/a"}");
        //Console.WriteLine($"Tile (9,9) region: {r2?.ToString() ?? "n/a"}");

        //// Get/Remove roundtrip
        //var got = await hub.GetObject(new GetObjectRequest { Id = "house_1" });
        //Console.WriteLine($"[gRPC] Get house_1 exists? {(got.Object != null)}");

        //var rem = await hub.RemoveObject(new RemoveObjectRequest { Id = "house_1" });
        //Console.WriteLine($"[gRPC] Removed house_1: {rem.Success}");

        //Console.WriteLine("Test run complete.");
    }

    private static async Task SafeRemove(IMapHub hub, string id)
    {
        try { await hub.RemoveObject(new RemoveObjectRequest { Id = id }); } catch { }
    }
}
