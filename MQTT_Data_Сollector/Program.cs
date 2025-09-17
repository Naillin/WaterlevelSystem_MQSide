﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTT_Data_Сollector.Core.Configs;
using MQTT_Data_Сollector.Core.Interfaces;
using MQTT_Data_Сollector.Implementations;
using MQTT_Data_Сollector.Services;
using MQTT_Data_Сollector.Workers;
using RabbitMQManager.Core.Implementations.RabbitMQ;
using RabbitMQManager.Core.Interfaces;
using RabbitMQManager.Core.Interfaces.MQ;
using RabbitMQManager.Core.Interfaces.MQ.RPC;
using RabbitMQManager.Implementations;
using RabbitMQManager.Implementations.RabbitMQ.RPC;

namespace MQTT_Data_Сollector
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
				// Получаем зависимости через DI
				var host = CreateHostBuilder(configurationRoot).Build();

				using (var startupScope = host.Services.CreateScope())
				{
					var serviceProvider = startupScope.ServiceProvider;

					var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

					var mqttClient = serviceProvider.GetRequiredService<IMqttClient>();
					await mqttClient.ConnectAsync(cts.Token);

					var rabbitMQConsumer = serviceProvider.GetRequiredService<IMessageConsumer>();
					await rabbitMQConsumer.ConnectAsync(cts.Token);
					var rabbitMQProducer = serviceProvider.GetRequiredService<IMessageProducer>();
					await rabbitMQProducer.ConnectAsync(cts.Token);
					var rabbitMQQueueManager = serviceProvider.GetRequiredService<IMessageQueueManager>();
					await rabbitMQQueueManager.ConnectAsync(cts.Token);

					var rabbitMQService = serviceProvider.GetRequiredService<RabbitMQService>();
					var mqttSubscriberWorker = serviceProvider.GetRequiredService<IWorker>();

					// Подписка на события
					mqttClient.MessageReceived += async (senderMQTT, eMQTT) =>
					{
						try
						{
							logger?.LogInformation($"Get data - Topic: [{eMQTT.Topic}] Message: [{eMQTT.Payload}]");

							if (!string.IsNullOrWhiteSpace(eMQTT.Topic) && !string.IsNullOrWhiteSpace(eMQTT.Payload))
								await rabbitMQService.PublishDataAsync(eMQTT.Topic, eMQTT.Payload);
							else
								logger?.LogWarning("MQTT message or topic is empty!");
						}
						catch (Exception ex)
						{
							logger?.LogError(ex, "Error in MQTT message receive!");
						}
					};

					await mqttSubscriberWorker.StartAsync();
				}

				Console.WriteLine("MQTT_Data_Сollector started. Press Ctrl+C to stop.");
				Console.WriteLine("Listening for messages...");

				// Ожидаем сигнал завершения
				await host.WaitForShutdownAsync(cts.Token);

				Console.WriteLine("MQTT_Data_Сollector stopped gracefully.");
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

				// Сервисы
				services.AddSingleton<IMqttClient, M2MqttClient>(provider =>
				{
					var config = provider.GetRequiredService<AppConfig>();
					var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
					var logger = loggerFactory.CreateLogger<M2MqttClient>();

					return new M2MqttClient(
						config.MQTT.Address,
						config.MQTT.Port,
						config.MQTT.Login,
						config.MQTT.Password,
						logger
					);
				});

				services.AddSingleton<IMessageProducer, RabbitMQProducer>(provider =>
				{
					var config = provider.GetRequiredService<AppConfig>();
					var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
					var logger = loggerFactory.CreateLogger<RabbitMQProducer>();
					var context = provider.GetRequiredService<MQConnectionContext>();

					return new RabbitMQProducer(logger, context, config.Rabbit.MQTTExchange);
				});
				services.AddSingleton<IMessageConsumer, RabbitMQConsumer>();
				services.AddSingleton<IMessageQueueManager, RabbitMQQueueManager>();

				services.AddSingleton<RabbitMQService>();
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
				services.AddSingleton<IWorker, MqttSubscriberWorker>();
			}).ConfigureLogging((context, logging) =>
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
