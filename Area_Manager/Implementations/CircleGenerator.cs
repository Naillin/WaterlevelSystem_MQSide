using Area_Manager.Core;

namespace Area_Manager.Implementations
{
	internal class CircleGenerator : PointsGenerator
	{
		private readonly Coordinate _center;
		public CircleGenerator(Coordinate center, double stepDistanceMeters = 30, double radiusMeters = 10000) : base(center, stepDistanceMeters, radiusMeters) => _center = center;

		public override List<Coordinate> Generate()
		{
			List<Coordinate> circlePoints = new List<Coordinate>();
			circlePoints.Add(_center); // Добавляем центр круга в результат

			// Генерация точек круга
			for (double currentRadiusDegrees = 0;
				currentRadiusDegrees <= radiusDegreesLat;
				currentRadiusDegrees += stepDistanceDegreesLat)
			{
				// Количество шагов для текущего радиуса
				int numberOfSteps = (int)(2 * Math.PI * currentRadiusDegrees / stepDistanceDegreesLat);

				for (int stepIndex = 0; stepIndex < numberOfSteps; stepIndex++)
				{
					// Угол для текущей точки
					double angle = stepIndex * (2 * Math.PI / numberOfSteps);

					// Вычисляем координаты точки
					double pointLat = _center.Latitude + currentRadiusDegrees * Math.Cos(angle); // Широта точки
					double pointLon = _center.Longitude + currentRadiusDegrees * Math.Sin(angle) * (stepDistanceDegreesLon / stepDistanceDegreesLat); // Долгота точки

					// Добавляем точку в результат
					circlePoints.Add(new Coordinate(pointLat, pointLon));
				}
			}

			return circlePoints;
		}
	}
}
