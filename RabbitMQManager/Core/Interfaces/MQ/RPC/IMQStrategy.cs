using RabbitMQManager.Core.Models;

namespace RabbitMQManager.Core.Interfaces.MQ.RPC
{
	public interface IMQStrategy
	{
		Task<ResponsePack> Use(string body, CancellationToken cancellationToken = default);
	}
}
