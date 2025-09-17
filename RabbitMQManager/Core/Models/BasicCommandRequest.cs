using RabbitMQManager.Core.Interfaces.MQ.RPC;

namespace RabbitMQManager.Core.Models
{
	internal class BasicCommandRequest : IMQRequest
	{
		public string? RequestId { get; set; }

		public string? QueueName { get; set; }

		public string? Type { get; set; }
	}
}
