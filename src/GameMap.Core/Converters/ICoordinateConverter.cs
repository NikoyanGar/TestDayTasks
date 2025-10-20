namespace GameMap.Core.Converters;

public interface ICoordinateConverter
{
    (double longitude, double latatitude) ToRedisCoordinates(int x, int y);

    public (int x, int y) FromRedisCoordinates(double longitude, double latatitude);
}
