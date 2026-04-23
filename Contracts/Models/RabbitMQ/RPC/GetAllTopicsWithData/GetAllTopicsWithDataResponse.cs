using RabbitMQManager.Core.Interfaces.MQ.RPC;

namespace Contracts.Models.RabbitMQ.RPC.GetAllTopicsWithData;

public class GetAllTopicsWithDataResponse : IMQResponse
{
    public string? RequestId { get; set; }

    public string? Type { get; set; } = "GetAllTopicsWithData";
    
    public bool Success { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public List<SensorDataDto>? Topics { get; set; } // Список топиков с данными
}