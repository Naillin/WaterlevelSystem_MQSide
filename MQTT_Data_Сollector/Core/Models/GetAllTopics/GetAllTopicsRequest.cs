using RabbitMQManager.Core.Interfaces.MQ.RPC;

namespace MQTT_Data_Сollector.Core.Models.GetAllTopics
{
	internal class GetAllTopicsRequest : IMQRequest
	{
		public string? RequestId { get; set; }

		public string? QueueName { get; set; }

		public string? Type { get; set; } = "GetAllTopicsRequest";
	}
}
