using RabbitMQManager.Core.Attributes;
using RabbitMQManager.Core.Interfaces.MQ.RPC;

namespace RabbitMQManager.Implementations.RabbitMQ.RPC
{
	public class CommandRegistry : ICommandRegistry
	{
		private readonly Dictionary<string, IMQStrategy> _strategies = new();

		public CommandRegistry(IEnumerable<IMQStrategy> strategies)
		{
			foreach (var strategy in strategies)
			{
				var type = strategy.GetType();
				var attrs = type.GetCustomAttributes(typeof(CommandAttribute), false)
								.Cast<CommandAttribute>();

				foreach (var attr in attrs)
				{
					_strategies[attr.Name] = strategy;
				}
			}
		}

		public bool TryGet(string commandName, out IMQStrategy? strategyType) => _strategies.TryGetValue(commandName, out strategyType);
	}
}
