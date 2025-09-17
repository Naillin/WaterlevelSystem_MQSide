using RabbitMQManager.Core.Interfaces.MQ.RPC;

namespace RabbitMQManager.Core.Models
{
	internal class BasicCommandResponse : IMQResponse
	{
		public string? RequestId { get; set; }

		public string? Type { get; set; }

		public bool Success { get; set; }

		public string? ErrorMessage { get; set; }
	}
}
