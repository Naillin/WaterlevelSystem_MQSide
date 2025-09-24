using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTT_Data_Сollector.Core.Interfaces;
using MQTT_Data_Сollector.Core.Models.GetAllTopics;
using RabbitMQManager.Core.Interfaces.MQ.RPC;

namespace MQTT_Data_Сollector.Workers
{
	internal class MqttSubscriberWorker : BackgroundService // в BackgroundService переделать
	{
		private readonly IMqttClient _mqttClient;
		private readonly IRPC_Client _rpcClient;
		private readonly ILogger<MqttSubscriberWorker> _logger;

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

		protected override async Task ExecuteAsync(CancellationToken stoppingToken = default)
		{
			try
			{
				while (!stoppingToken.IsCancellationRequested)
				{
					await RefreshSubscriptions(stoppingToken);
					//await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
					await Task.Delay(30000, stoppingToken);
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
			finally
			{
				await _mqttClient.UnsubscribeAllAsync();
				await _mqttClient.DisconnectAsync();
				_mqttClient.Dispose();
				_rpcClient.Dispose();
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
	}
}
