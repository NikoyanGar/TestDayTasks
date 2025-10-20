using GameMap.Core.Converters;

sealed class GridCoordinateConverter : ICoordinateConverter
{
    public (int x, int y) FromRedisCoordinates(double longitude, double latatitude)
    {
        throw new NotImplementedException();
    }

    public (double longitude, double latatitude) ToRedisCoordinates(int x, int y) => (x, y);
}