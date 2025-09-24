using Area_Manager.Core.Interfaces;

namespace Area_Manager.Core
{
	internal abstract class PointsGenerator : IPointsGenerator
	{
		protected double stepDistanceDegreesLat;
		protected double radiusDegreesLat;

		protected double centerLatRadians;
		protected double metersPerDegreeLon;
		protected double stepDistanceDegreesLon;
		protected double radiusDegreesLon;

		public PointsGenerator(Coordinate center, double stepDistanceMeters = 30, double radiusMeters = 10000)
		{
			// Преобразуем метры в градусы для широты (1 градус широты ≈ 111320 метров)
			stepDistanceDegreesLat = stepDistanceMeters / 111320.0;
			radiusDegreesLat = radiusMeters / 111320.0;

			// Учитываем, что длина градуса долготы зависит от широты
			centerLatRadians = center.Latitude * Math.PI / 180.0; // Широта центра в радианах
			metersPerDegreeLon = 111320.0 * Math.Cos(centerLatRadians); // Длина градуса долготы на текущей широте
			stepDistanceDegreesLon = stepDistanceMeters / metersPerDegreeLon; // Шаг для долготы в градусах
			radiusDegreesLon = radiusMeters / metersPerDegreeLon; // Радиус для долготы в градусах
		}

		public abstract List<Coordinate> Generate();
	}
}
