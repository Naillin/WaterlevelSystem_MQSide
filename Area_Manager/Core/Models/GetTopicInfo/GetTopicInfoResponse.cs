using RabbitMQManager.Core.Interfaces.MQ.RPC;

namespace Area_Manager.Core.Models.GetTopicInfo
{
	internal class GetTopicInfoResponse : IMQResponse
	{
		public string? RequestId { get; set; }

		public string? Type { get; set; } = "GetTopicInfo";

		public bool Success { get; set; }

		public string? ErrorMessage { get; set; }

		public SensorDataDto? TopicInfo { get; set; } // Список путей топиков
	}
}
