namespace RabbitMQManager.Core.Interfaces.MQ.RPC
{
	public interface IMQRequest
	{
		public string? RequestId { get; set; } // Уникальный ID запроса

		public string? QueueName { get; set; } // Куда писать

		public string? Type { get; set; } // Тип запроса
	}
}
