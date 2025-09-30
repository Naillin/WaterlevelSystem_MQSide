using RabbitMQManager.Core.Interfaces.MQ.RPC;

namespace Area_Manager.Core.Models.GetTopicInfo
{
	internal class GetTopicInfoRequest : IMQRequest
	{
		public GetTopicInfoRequest(string topicPath) => this.topicPath = topicPath;

		public string? RequestId { get; set; }

		public string? QueueName { get; set; }

		public string? Type { get; set; } = "GetTopicInfo";

		public string? topicPath { get; set; }
	}
}
