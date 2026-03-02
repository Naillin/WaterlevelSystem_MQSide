using Area_Manager.Core.Models;

namespace Area_Manager.Core.Interfaces
{
	internal interface ISensorDataService
	{
		Task AddData(SensorDataReceivedEvent sensorEvent, CancellationToken cancellationToken = default);

		IReadOnlyDictionary<string, SensorDataDto> GetSensorData();

		void DeleteSensorsAsync(IList<string> topicKeys);
	}
}
