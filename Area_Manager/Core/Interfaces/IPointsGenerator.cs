namespace Area_Manager.Core.Interfaces
{
	internal interface IPointsGenerator
	{
		abstract List<Coordinate> Generate();

		IPointsGenerator Prepare(Coordinate center, double radiusMeters = 10000, double stepDistanceMeters = 30);
	}
}
