using RabbitMQManager.Core.Interfaces.MQ;
using RabbitMQManager.Implementations;

namespace MQGateway.Workers
{
	internal class ConnectorWorker : MQConnectorWorker
	{
		public ConnectorWorker(
			IMessageConsumer? messageConsumer = default,
			IMessageProducer? messageProducer = default,
			IMessageQueueManager? messageQueueManager = default) : base (
				messageConsumer,
				messageProducer,
				messageQueueManager)
		{ }
	}
}
