using DispatcherAreaManager.Core.Models;

namespace DispatcherAreaManager.Core.Interfaces
{
	internal interface IQueueManagerService
	{
		public void AddData(SensorDataReceivedEvent sensorEvent);

		public IReadOnlyDictionary<string, Queue<SensorDataReceivedEvent>> GetSensors();
	}
}
