using Contracts.Models;

namespace Area_Manager.Core.Interfaces;

public interface ISensorCacheService
{
    Task<IList<SensorDataDto>?> GetAllSensorsWithData(CancellationToken cancellationToken = default);
}