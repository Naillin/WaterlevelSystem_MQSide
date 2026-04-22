using RabbitMQManager.Core.Models;
using RabbitMQManager.Implementations;

namespace RabbitMQManager.Core.Interfaces.MQ.RPC;

public interface IRPC_Handler
{
	Task<ResponsePack> Handle(MessageContext context, CancellationToken cancellationToken = default);
}