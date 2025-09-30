using RabbitMQManager.Core.Interfaces.MQ.RPC;

namespace MQGateway.Core.Models.GetAllTopics
{
	internal class GetAllTopicsRequest : IMQRequest
	{
		public string? RequestId { get; set; }

		public string? QueueName { get; set; }

		public string? Type { get; set; } = "GetAllTopics";
	}
}
