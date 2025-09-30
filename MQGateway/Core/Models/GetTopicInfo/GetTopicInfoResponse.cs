using RabbitMQManager.Core.Interfaces.MQ.RPC;

namespace MQGateway.Core.Models.GetTopicInfo
{
	internal class GetTopicInfoResponse : IMQResponse
	{
		public string? RequestId { get; set; }

		public string? Type { get; set; } = "GetTopicInfo";

		public bool Success { get; set; }

		public string? ErrorMessage { get; set; }

		public string TopicPath { get; set; } = string.Empty;

		public double Latitude { get; set; }

		public double Longitude { get; set; }

		public double Altitude { get; set; } = 0.0;
	}
}
