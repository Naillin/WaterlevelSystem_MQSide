namespace RabbitMQManager.Core.Interfaces.MQ.RPC
{
	public interface ICommandRegistry
	{
		bool TryGet(string commandName, out IMQStrategy? strategyType);
	}
}
