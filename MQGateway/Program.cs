using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQGateway.Core.Interfaces;
using MQGateway.DataWork;
using MQGateway.DataWork.Repositories;
using MQGateway.Strategies;
using MQGateway.Workers;
using MQTT_Data_Сollector.Core.Configs;
using RabbitMQManager.Core.Attributes;
using RabbitMQManager.Core.Implementations.RabbitMQ;
using RabbitMQManager.Core.Interfaces.MQ;
using RabbitMQManager.Core.Interfaces.MQ.RPC;
using RabbitMQManager.Implementations;
using RabbitMQManager.Implementations.RabbitMQ.RPC;
using System.Reflection;

namespace MQGateway
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			// Конфиг
			var configurationRoot = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true)
				.AddEnvironmentVariables()
				.Build();

			using var cts = new CancellationTokenSource();
			Console.CancelKeyPress += (s, e) =>
			{
				e.Cancel = true; // Предотвращаем немедленное завершение процесса
				Console.WriteLine("\nShutdown initiated...");
				cts.Cancel();
			};

			try
			{
				var host = CreateHostBuilder(configurationRoot).Build();

				using (var startupScope = host.Services.CreateScope())
				{
					var serviceProvider = startupScope.ServiceProvider;

					var messageConsumer = serviceProvider.GetRequiredService<IMessageConsumer>();
					await messageConsumer.ConnectAsync(cts.Token);
					var messageProducer = serviceProvider.GetRequiredService<IMessageProducer>();
					await messageProducer.ConnectAsync(cts.Token);

					var collectorWorker = host.Services.GetRequiredService<CollectorWorker>();
					await collectorWorker.StartAsync(cts.Token);

					var commander = serviceProvider.GetRequiredService<RabbitMQ_RPC_Commander>();
					await commander.StartAsync(cts.Token);
				}

				Console.WriteLine("Application started. Press Ctrl+C to stop.");
				Console.WriteLine("Listening for messages...");

				// Ожидаем сигнал завершения
				await host.WaitForShutdownAsync(cts.Token);

				Console.WriteLine("Application stopped gracefully.");
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

				services.AddSingleton(provider =>
				{
					var config = provider.GetRequiredService<AppConfig>();
					return ConfigMQConnect(config.Rabbit);
				});

				// Регистрируем сервисы
				services.AddDbContextFactory<AppDbContext>((provider, options) =>
				{
					var config = provider.GetRequiredService<AppConfig>();
					options.UseNpgsql(config.Database.ConnectionString);
				});
				services.AddScoped<IDataRepository, DatabaseRepository>();

				services.AddSingleton<IMessageConsumer, RabbitMQConsumer>(); // общий потребитель
				services.AddSingleton<IMessageProducer, RabbitMQProducer>(); //общий продюсер exchange не указан

				services.AddSingleton<CollectorWorker>(provider =>
				{
					var config = provider.GetRequiredService<AppConfig>();
					var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
					var logger = loggerFactory.CreateLogger<CollectorWorker>();
					var consumer = provider.GetRequiredService<IMessageConsumer>();
					var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

					return new CollectorWorker(logger, consumer, scopeFactory, config.Rabbit.MQTTQueue);
				});

				//RPC Strategy
				var strategyTypes = typeof(GetTopics)
					.Assembly
					.GetTypes()
					.Where(t => typeof(IMQStrategy).IsAssignableFrom(t) &&
						!t.IsAbstract &&
						!t.IsInterface &&
						t.GetCustomAttribute<CommandAttribute>() != null);
				foreach (var type in strategyTypes)
				{
					services.AddTransient(typeof(IMQStrategy), type);
					services.AddTransient(type);
				}

				services.AddSingleton<ICommandRegistry, CommandRegistry>();
				services.AddSingleton<IRPC_Handler, RabbitMQ_RPC_Handler>();
				services.AddSingleton<RabbitMQ_RPC_Commander>(provider =>
				{
					var config = provider.GetRequiredService<AppConfig>();
					var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
					var logger = loggerFactory.CreateLogger<RabbitMQ_RPC_Commander>();
					var consumer = provider.GetRequiredService<IMessageConsumer>();
					var producer = provider.GetRequiredService<IMessageProducer>();
					var handler = provider.GetRequiredService<IRPC_Handler>();

					return new RabbitMQ_RPC_Commander(logger, consumer, producer, handler, config.Rabbit.RPC_Queue);
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
	}
}
