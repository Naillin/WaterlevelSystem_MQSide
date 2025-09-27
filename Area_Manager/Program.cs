using Area_Manager.Core.Configs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQManager.Implementations;

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

				services.AddSingleton(provider =>
				{
					var config = provider.GetRequiredService<AppConfig>();
					return ConfigMQConnect(config.Rabbit);
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
