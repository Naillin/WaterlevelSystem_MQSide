using Contracts.Models;

namespace Area_Manager.Core.Interfaces;

internal interface IFloodDataService
{
	Task<AnalysisPack> Analysis(SensorDataDto sensorData, CancellationToken cancellationToken = default);
}
public record AnalysisPack (IList<Coordinate>? Coordinates, IList<ValueAtTime>? Smoothed, IList<ValueAtTime>? Predictions);
