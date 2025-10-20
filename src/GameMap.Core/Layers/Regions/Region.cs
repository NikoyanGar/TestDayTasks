namespace GameMap.Core.Features.Regions;

public sealed class Region
{
    public ushort Id { get; }
    public string Name { get; }

    public Region(ushort id, string name)
    {
        Id = id;
        Name = name;
    }
}
