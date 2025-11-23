using DispatcherAreaManager.Core.Models;

namespace DispatcherAreaManager.Core.Interfaces
{
	internal interface IKubernetesService
	{
		void CreatePod();

		void DeletePod();

		//void AddData(SensorDataReceivedEvent sensorData);
	}
}
