using Contracts.Models;

namespace Area_Manager.Core.Interfaces;

public interface IGDALPython
{
    public void StartPythonProcess();
    
    public bool HealthCheck();
    
    public Task<double> GetElevation(Coordinate coordinate, CancellationToken cancellationToken = default);
}