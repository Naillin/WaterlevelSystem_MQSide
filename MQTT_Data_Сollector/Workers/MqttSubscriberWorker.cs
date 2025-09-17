using Microsoft.Extensions.Logging;
using MQTT_Data_Сollector.Core.Interfaces;
using MQTT_Data_Сollector.Core.Models.GetAllTopics;
using RabbitMQManager.Core.Interfaces;
using RabbitMQManager.Core.Interfaces.MQ.RPC;

namespace MQTT_Data_Сollector.Workers
{
	internal class MqttSubscriberWorker : IWorker
	{
		private readonly IMqttClient _mqttClient;
		private readonly IRPC_Client _rpcClient;
		private readonly ILogger<MqttSubscriberWorker> _logger;

		private CancellationTokenSource? _cts;
		private Task? _runningTask;
		private HashSet<string> _currentSubscriptions = new();

		public MqttSubscriberWorker(
			IMqttClient mqttClient,
			IRPC_Client rpcClient,
			ILogger<MqttSubscriberWorker> logger)
		{
			_mqttClient = mqttClient;
			_rpcClient = rpcClient;
			_logger = logger;
		}

		public Task StartAsync(CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Starting MQTT subscriber worker");
			_cts = new CancellationTokenSource();
			_runningTask = RunAsync(_cts.Token);
			return _runningTask;
		}

		public async Task StopAsync(CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Stopping MQTT subscriber worker");
			_cts?.Cancel();

			if (_runningTask != null)
			{
				await _runningTask;
			}

			await _mqttClient.UnsubscribeAllAsync();
			_logger.LogInformation("MQTT subscriber worker stopped");
		}

		private async Task RunAsync(CancellationToken cancellationToken)
		{
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					await RefreshSubscriptions(cancellationToken);
					//await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
					await Task.Delay(30000, cancellationToken);
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("Worker operation cancelled");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in MQTT subscriber worker");
			}
		}

		private async Task RefreshSubscriptions(CancellationToken cancellationToken)
		{
			try
			{
				_logger.LogInformation("Refreshing topic subscriptions");

				var response = await _rpcClient.SendRequestAsync<GetAllTopicsRequest, GetAllTopicsResponse>(
					new GetAllTopicsRequest(),
					"GetAllTopicsRequest",
					TimeSpan.FromSeconds(30),
					cancellationToken
				);
				//убрать хардкод!!!!!!!!!!!!!!!!!!!

				if (!response.Success)
				{
					_logger.LogError($"Failed to get topics: {response.ErrorMessage}");
					return;
				}

				if (response.Topics == null || response.Topics.Count == 0)
				{
					_logger.LogWarning("Received empty topics list");
					return;
				}

				await UpdateSubscriptions(response.Topics);
			}
			catch (TimeoutException ex)
			{
				_logger.LogWarning(ex.Message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error refreshing subscriptions");
			}
		}

		private async Task UpdateSubscriptions(List<string> newTopics)
		{
			var newTopicsSet = new HashSet<string>(newTopics);
			var topicsToSubscribe = newTopicsSet.Except(_currentSubscriptions).ToList();
			var topicsToUnsubscribe = _currentSubscriptions.Except(newTopicsSet).ToList();

			if (topicsToSubscribe.Any())
			{
				await _mqttClient.SubscribeAsync(topicsToSubscribe.ToArray());
				_logger.LogInformation($"Subscribed to {topicsToSubscribe.Count} new topics");
			}

			if (topicsToUnsubscribe.Any())
			{
				await _mqttClient.UnsubscribeAsync(topicsToUnsubscribe.ToArray());
				_logger.LogInformation($"Unsubscribed from {topicsToUnsubscribe.Count} old topics");
			}

			_currentSubscriptions = newTopicsSet;
			_logger.LogInformation($"Total subscriptions: {_currentSubscriptions.Count}");
		}

		public void Dispose()
		{
			StopAsync().GetAwaiter();
			_cts?.Dispose();
			_mqttClient.Dispose();
		}
	}
}
