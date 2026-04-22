using RabbitMQManager.Core.Interfaces.MQ.RPC;

namespace Contracts.Models.RabbitMQ.RPC.GetAllTopics;

public class GetAllTopicsRequest : IMQRequest
{
	public string? RequestId { get; set; }

	public string? QueueName { get; set; }

	public string? Type { get; set; } = "GetAllTopics";
}