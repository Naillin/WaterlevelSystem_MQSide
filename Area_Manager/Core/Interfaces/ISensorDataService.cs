using Contracts.Models;
using Contracts.Models.RabbitMQ;

namespace Area_Manager.Core.Interfaces;

internal interface ISensorDataService
{
    Task LoadData(CancellationToken cancellationToken = default);
    
    Task AddData(SensorDataReceivedEvent sensorEvent, CancellationToken cancellationToken = default);

    IReadOnlyDictionary<string, SensorDataDto> GetSensorData();

    void DeleteSensorsAsync(IList<string> topicKeys);
}
