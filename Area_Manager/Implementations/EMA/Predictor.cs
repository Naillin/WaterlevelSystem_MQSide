using Area_Manager.Core.Interfaces.EMA;
using Contracts.Models;

namespace Area_Manager.Implementations.EMA;

internal class Predictor : IPredictor
{
	private readonly ITrendCalculator _trendCalculator;
	private readonly double _trendFactor;

	public Predictor(ITrendCalculator trendCalculator, double trendFactor = 1.0)
	{
		_trendCalculator = trendCalculator;
		_trendFactor = trendFactor;
	}

	public IList<ValueAtTime> Predict(IList<ValueAtTime> values, int predictionSteps = 3)
	{
		if (values.Count < 2)
			return Array.Empty<ValueAtTime>();
			
		var windowValues = values
			.TakeLast(Math.Min(10, values.Count))
			.ToList();

		var trend = _trendCalculator.CalculateTrend(windowValues.Select(value => value.Value).ToList()) * _trendFactor;
		var last = windowValues.Last();
			
		double avgSeconds = (last.DateTime - windowValues.First().DateTime).TotalSeconds / (windowValues.Count - 1);
		TimeSpan averageTimeInterval = TimeSpan.FromSeconds(avgSeconds);
			
		var predictions = new List<ValueAtTime>();
		for (int i = 1; i <= predictionSteps; i++)
		{
			var dataUnit = new ValueAtTime(
				last.Value + trend * i,
				last.DateTime + averageTimeInterval * i
			);
			predictions.Add(dataUnit);
		}
			
		return predictions;
	}
}