using Area_Manager.Core.Models;

namespace Area_Manager.Core.Interfaces
{
	internal interface IFloodDataService
	{
		Task<(IList<Coordinate> coordinates, IList<double> smoothed, IList<ValueAtTime> predictions)> Analysis(SensorDataDto sensorData, CancellationToken cancellationToken = default);
	}
}
