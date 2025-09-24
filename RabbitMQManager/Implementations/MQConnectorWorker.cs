using Microsoft.Extensions.Hosting;
using RabbitMQManager.Core.Interfaces.MQ;

namespace RabbitMQManager.Implementations
{
	public class MQConnectorWorker : IHostedService
	{
		private readonly IMessageConsumer? _messageConsumer;
		private readonly IMessageProducer? _messageProducer;
		private readonly IMessageQueueManager? _messageQueueManager;

		public MQConnectorWorker(
			IMessageConsumer? messageConsumer = default,
			IMessageProducer? messageProducer = default,
			IMessageQueueManager? messageQueueManager = default)
		{
			_messageConsumer = messageConsumer;
			_messageProducer = messageProducer;
			_messageQueueManager = messageQueueManager;
		}

		public virtual async Task StartAsync(CancellationToken cancellationToken = default)
		{
			if (_messageConsumer != null)
				await _messageConsumer.ConnectAsync(cancellationToken);
			if (_messageProducer != null)
				await _messageProducer.ConnectAsync(cancellationToken);
			if (_messageQueueManager != null)
				await _messageQueueManager.ConnectAsync(cancellationToken);
		}

		public virtual async Task StopAsync(CancellationToken cancellationToken = default)
		{
			if (_messageConsumer != null)
				await _messageConsumer.DisconnectAsync(cancellationToken);
			if (_messageProducer != null)
				await _messageProducer.DisconnectAsync(cancellationToken);
			if (_messageQueueManager != null)
				await _messageQueueManager.DisconnectAsync(cancellationToken);
		}
	}
}
