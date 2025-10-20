namespace GameMap.Shared.Contracts;

public enum NetMessageType : byte
{
    GetObjectsInAreaRequest = 1,
    GetObjectsInAreaResponse = 2,
    GetRegionsInAreaRequest = 3,
    GetRegionsInAreaResponse = 4,
    ObjectAdded = 10,
    ObjectUpdated = 11,
    ObjectDeleted = 12
}
