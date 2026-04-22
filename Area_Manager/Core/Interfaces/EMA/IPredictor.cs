using Contracts.Models;

namespace Area_Manager.Core.Interfaces.EMA;

internal interface IPredictor
{
	public IList<ValueAtTime> Predict(IList<ValueAtTime> values, int predictionSteps = 3);
}
