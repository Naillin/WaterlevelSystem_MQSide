using Area_Manager.Core.Models;

namespace Area_Manager.Core.Interfaces
{
	internal interface IFloodDataService
	{
		Task<IList<Coordinate>> Analysis(SensorDataDto sensorData, CancellationToken cancellationToken = default);
	}
}
