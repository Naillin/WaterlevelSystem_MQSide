using RabbitMQManager.Core.Implementations;
using RabbitMQManager.Core.Models;

namespace RabbitMQManager.Core.Interfaces.MQ.RPC
{
	public interface IRPC_Handler
	{
		Task<ResponsePack> Handle(MessageContext context, CancellationToken cancellationToken = default);
	}
}
