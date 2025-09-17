namespace RabbitMQManager.Core.Interfaces.MQ.RPC
{
	public interface IMQResponse
	{
		string? RequestId { get; set; } // Должен совпадать с ID запроса

		string? Type { get; set; } // Куда писать

		bool Success { get; set; }

		string? ErrorMessage { get; set; }
	}
}
