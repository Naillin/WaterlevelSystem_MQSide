using Contracts.Models;
using Contracts.Models.RabbitMQ;

namespace Area_Manager.Core.Interfaces;

internal interface ISensorDataService
{
    Task AddData(SensorDataReceivedEvent sensorEvent, CancellationToken cancellationToken = default);

    IReadOnlyDictionary<string, SensorDataDto> GetSensorData();

    void DeleteSensorsAsync(IList<string> topicKeys);
}
