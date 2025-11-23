using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQManager.Core.Interfaces.MQ;

namespace DispatcherAreaManager.Implementations.Workers
{
	internal class KuberWorker : BackgroundService
	{
		private readonly ILogger<KuberWorker> _logger;
		private readonly IMessageProducer _messageProducer;

		public KuberWorker(ILogger<KuberWorker> logger, IMessageProducer messageProducer)
		{
			_messageProducer = messageProducer;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken = default)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				_logger.LogInformation($"Pushing data sensors");
				//await Push(stoppingToken);

				await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
			}
		}
	}
}
