using DispatcherAreaManager.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DispatcherAreaManager.Implementations.Workers
{
	internal class KuberWorker : BackgroundService
	{
		private readonly ILogger<KuberWorker> _logger;
		private IQueueManagerService _queueManagerService;

		public KuberWorker(ILogger<KuberWorker> logger, IQueueManagerService queueManagerService)
		{
			_queueManagerService = queueManagerService;
			
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
