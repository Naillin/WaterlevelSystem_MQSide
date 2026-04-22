using RabbitMQManager.Core.Interfaces.MQ.RPC;

namespace Contracts.Models.RabbitMQ.RPC.GetAllTopics;

public class GetAllTopicsResponse : IMQResponse
{
	public string? RequestId { get; set; }

	public string? Type { get; set; } = "GetAllTopics";

	public bool Success { get; set; }

	public string? ErrorMessage { get; set; }

	public List<string>? Topics { get; set; } // Список путей топиков
}