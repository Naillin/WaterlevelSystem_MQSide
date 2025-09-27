using Area_Manager.Core.Interfaces;

namespace Area_Manager.Core
{
	internal abstract class PointsGenerator : IPointsGenerator
	{
		//protected double _stepDistanceMeters;
		//protected double _radiusMeters;
		protected Coordinate _center;

		protected double _stepDistanceDegreesLat;
		protected double _radiusDegreesLat;

		protected double _centerLatRadians;
		protected double _metersPerDegreeLon;
		protected double _stepDistanceDegreesLon;
		protected double _radiusDegreesLon;

		public abstract List<Coordinate> Generate();

		public virtual IPointsGenerator Prepare(Coordinate center, double radiusMeters = 10000, double stepDistanceMeters = 30)
		{
			_center = center;

			// Преобразуем метры в градусы для широты (1 градус широты ≈ 111320 метров)
			_stepDistanceDegreesLat = stepDistanceMeters / 111320.0;
			_radiusDegreesLat = radiusMeters / 111320.0;

			// Учитываем, что длина градуса долготы зависит от широты
			_centerLatRadians = _center.Latitude * Math.PI / 180.0; // Широта центра в радианах
			_metersPerDegreeLon = 111320.0 * Math.Cos(_centerLatRadians); // Длина градуса долготы на текущей широте
			_stepDistanceDegreesLon = stepDistanceMeters / _metersPerDegreeLon; // Шаг для долготы в градусах
			_radiusDegreesLon = radiusMeters / _metersPerDegreeLon; // Радиус для долготы в градусах

			return this;
		}
	}
}
