using Area_Manager.Core.Models;

namespace Area_Manager.Core.Interfaces
{
	internal interface ISensorDataService
	{
		public Task AddData(SensorDataReceivedEvent sensorEvent, CancellationToken cancellationToken = default);

		public IReadOnlyDictionary<string, SensorDataDto> GetSensorData();
	}
}
