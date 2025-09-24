namespace Area_Manager.Core.Interfaces
{
	internal interface IPredictor
	{
		(List<double> smoothedValues, List<double> predictions) Predict(List<double> values, int predictionSteps = 3);
	}
}
