using Area_Manager.Core.Configs;
using Area_Manager.Core.Interfaces;
using Area_Manager.Core.Interfaces.EMA;
using Area_Manager.Implementations;
using Area_Manager.Implementations.EMA;
using Area_Manager.Implementations.Metrics;
using Area_Manager.Services;
using Area_Manager.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQManager.Core.Implementations.RabbitMQ;
using RabbitMQManager.Core.Interfaces.MQ;
using RabbitMQManager.Core.Interfaces.MQ.RPC;
using RabbitMQManager.Implementations;
using RabbitMQManager.Implementations.RabbitMQ.RPC;

namespace Area_Manager
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			using var cts = new CancellationTokenSource();
			Console.CancelKeyPress += (s, e) =>
			{
				e.Cancel = true; // Предотвращаем немедленное завершение процесса
				Console.WriteLine("\nShutdown initiated...");
				cts.Cancel();
			};

			try
			{
				// Получаем зависимости через DI
				var host = CreateHostBuilder(GetConfig()).Build();
				await host.RunAsync(cts.Token);

				Console.WriteLine("Area_Manager started. Press Ctrl+C to stop.");
				Console.WriteLine("Listening for messages...");

				// Ожидаем сигнал завершения
				await host.WaitForShutdownAsync(cts.Token);

				Console.WriteLine("Area_Manager stopped gracefully.");
			}
			catch (OperationCanceledException)
			{
				Console.WriteLine("Shutdown completed.");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Fatal error: {ex.Message}");
				Environment.Exit(1);
			}
		}

		private static IHostBuilder CreateHostBuilder(IConfigurationRoot configurationRoot) => Host.CreateDefaultBuilder()
			.ConfigureServices((context, services) =>
			{
				// Конфиг
				services.Configure<AppConfig>(configurationRoot);
				services.AddSingleton<AppConfig>(sp => sp.GetRequiredService<IOptions<AppConfig>>().Value);

				services.AddSingleton<MQConnectionContext>(provider =>
				{
					var config = provider.GetRequiredService<AppConfig>();
					return ConfigMQConnect(config.Rabbit);
				});

				services.AddSingleton<IMessageConsumer, RabbitMQConsumer>(); // общий потребитель
				services.AddSingleton<IMessageProducer, RabbitMQProducer>(provider =>
				{
					var config = provider.GetRequiredService<AppConfig>();
					var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
					var logger = loggerFactory.CreateLogger<RabbitMQProducer>();
					var context = provider.GetRequiredService<MQConnectionContext>();

					return new RabbitMQProducer(logger, context, config.Rabbit.Flood_Exchange);
				});
				services.AddSingleton<IMessageQueueManager, RabbitMQQueueManager>(); //общий продюсер exchange не указан
				services.AddSingleton<IRPC_Client, RabbitMQ_RPC_Client>(provider =>
				{
					var config = provider.GetRequiredService<AppConfig>();
					var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
					var logger = loggerFactory.CreateLogger<RabbitMQ_RPC_Client>();
					var queueManager = provider.GetRequiredService<IMessageQueueManager>();
					var consumer = provider.GetRequiredService<IMessageConsumer>();
					var producer = provider.GetRequiredService<IMessageProducer>();

					return new RabbitMQ_RPC_Client(logger, queueManager, consumer, producer, config.Rabbit.RPC_Exchange, config.Rabbit.RPC_Routing);
				});
				services.AddHostedService<MQConnectorWorker>();

				services.AddSingleton<ITrendCalculator, LinearTrendCalculator>();
				services.AddSingleton<IMovingAverage, ExponentialMovingAverage>();
				services.AddSingleton<IPredictor, EMAPredictor>();

				services.AddSingleton<IPointsGenerator, CircleGenerator>();
				services.AddSingleton<IAreaCalculator, PythonAreaCalculator>();

				services.AddSingleton<IMetric, MetricMAE>();

				services.AddSingleton<IFloodDataService, FloodDataService>();
				services.AddSingleton<ISensorDataService, SensorDataService>();

				services.AddHostedService<FloodWorker>();
				services.AddHostedService<SensorDataWorker>(provider =>
				{
					var config = provider.GetRequiredService<AppConfig>();
					var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
					var logger = loggerFactory.CreateLogger<SensorDataWorker>();
					var consumer = provider.GetRequiredService<IMessageConsumer>();
					var sensorDataService = provider.GetRequiredService<ISensorDataService>();

					return new SensorDataWorker(config.Rabbit.Analyzer_Queue, consumer, sensorDataService, logger);
				});
			})
			.ConfigureLogging((context, logging) =>
			{
				logging.ClearProviders();

				if (OperatingSystem.IsLinux())
					logging.AddSystemdConsole().SetMinimumLevel(LogLevel.Information);
				else
					logging.AddConsole().SetMinimumLevel(LogLevel.Information);
			});

		private static MQConnectionContext ConfigMQConnect(RabbitConfig config) => new MQConnectionContext(config.Address, config.Port, config.Login, config.Password, config.VirtualHost);

		private static IConfigurationRoot GetConfig() =>
			new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true)
			.AddEnvironmentVariables()
			.Build();
	}
}
