using RabbitMQManager.Core.Interfaces.MQ.RPC;

namespace MQGateway.Core.Models.GetAllTopics
{
	internal class GetAllTopicsResponse : IMQResponse
	{
		public string? RequestId { get; set; }

		public string? Type { get; set; } = "GetAllTopicsResponse";

		public bool Success { get; set; }

		public string? ErrorMessage { get; set; }

		public List<string>? Topics { get; set; } // Список путей топиков
	}
}
