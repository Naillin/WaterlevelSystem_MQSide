using Area_Manager.Core;
using Area_Manager.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Area_Manager.Implementations
{
	internal class PythonAreaCalculator : IAreaCalculator
	{
		private readonly ILogger<PythonAreaCalculator> _logger;
		private readonly IPointsGenerator _pointsGenerator;

		//хардкод убрать в всех считалках!!!!
		private double _distance = 200;
		private double _radius = 10000;
		private int _countOfSubs = 100;
		private double _coefHeight = 2.0;

		private string _pythonPath = "GDALPython/venv/bin/python3";
		private string _scriptPath = "GDALPython/main.py";

		public PythonAreaCalculator(ILogger<PythonAreaCalculator> logger)
		{
			_pointsGenerator = new CircleGenerator();

			_logger = logger;
		}

		public List<Coordinate> FindArea(Coordinate coordinate, double initialHeight = 100)
		{
			List<Coordinate> result = new List<Coordinate>();
			HashSet<Coordinate> checkedPoints = new HashSet<Coordinate>();

			double stepForHeight = (initialHeight / _coefHeight) / (double)_countOfSubs;
			double stepForRadius = _radius / (double)_countOfSubs;

			using (var _gDALPython = new GDALPython.GDALPython(_pythonPath, _scriptPath))
			{
				for (double currentRadius = stepForRadius; currentRadius <= 10000; currentRadius = currentRadius + stepForRadius)
				{
					List<Coordinate> circleCoordinates = _pointsGenerator.Prepare(coordinate, currentRadius, _distance).Generate();
					foreach (Coordinate item in circleCoordinates)
					{
						if (checkedPoints.Contains(item))
							continue;
						
						checkedPoints.Add(item);

						double currentElevation = _gDALPython.GetElevation(item);
						//logger.Info($"Высота проверяемой точки: {currentElevation}.");
						if (currentElevation <= initialHeight)
							result.Add(item);
						
					}

					initialHeight = initialHeight - stepForHeight;
				}
			}

			return result;
		}
	}
}
