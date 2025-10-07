using Area_Manager.Core;
using Area_Manager.Core.Interfaces;
using Area_Manager.Core.Models;
using Area_Manager.Core.Models.GetTopicInfo;
using Microsoft.Extensions.Logging;
using RabbitMQManager.Core.Interfaces.MQ.RPC;
using System.Collections.Concurrent;

namespace Area_Manager.Services
{
	internal class SensorDataService : ISensorDataService
	{
		private readonly ILogger<SensorDataService> _logger;
		private readonly IRPC_Client _rpcClient;

		private readonly ConcurrentDictionary<string, Lazy<Task<SensorDataDto>>> _sensorDataTasks = new();
		private readonly ConcurrentDictionary<string, List<(double, long)>> _pendingData = new();

		public SensorDataService(IRPC_Client rpcClient, ILogger<SensorDataService> logger)
		{
			_rpcClient = rpcClient;

			_logger = logger;
		}

		public IReadOnlyDictionary<string, SensorDataDto> GetSensorData()
		{
			// Возвращаем только завершенные сенсоры
			var result = new Dictionary<string, SensorDataDto>();

			foreach (var kvp in _sensorDataTasks)
			{
				if (kvp.Value.IsValueCreated && kvp.Value.Value.IsCompletedSuccessfully)
				{
					result[kvp.Key] = kvp.Value.Value.Result;
				}
				// Можно также добавить логику для сенсоров, которые еще в процессе создания
			}

			return result;
		}

		public Task AddData(SensorDataReceivedEvent sensorEvent, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(sensorEvent.TopicPath))
				return Task.CompletedTask;

			var topicPath = sensorEvent.TopicPath!;

			// БЫСТРЫЙ ПУТЬ: сенсор уже полностью создан
			if (IsSensorFullyCreated(topicPath))
			{
				AddDataToExistingSensor(topicPath, sensorEvent.Value, sensorEvent.Timestamp);
				return Task.CompletedTask;
			}

			// МЕДЛЕННЫЙ ПУТЬ: сенсор еще не готов
			_pendingData.AddOrUpdate(topicPath,
				new List<(double, long)> { (sensorEvent.Value, sensorEvent.Timestamp) },
				(key, existing) =>
				{
					existing.Add((sensorEvent.Value, sensorEvent.Timestamp));
					return existing;
				});

			// Запускаем фоновую инициализацию fire-and-forget
			_ = Task.Run(() => TryEnsureSensorInitializedAsync(topicPath, cancellationToken));

			return Task.CompletedTask;
		}

		private bool IsSensorFullyCreated(string topicPath) =>
			_sensorDataTasks.TryGetValue(topicPath, out var lazyTask) &&
			lazyTask.IsValueCreated &&
			lazyTask.Value.IsCompletedSuccessfully;

		private void AddDataToExistingSensor(string topicPath, double value, long timestamp)
		{
			if (_sensorDataTasks.TryGetValue(topicPath, out var lazyTask) &&
				lazyTask.IsValueCreated &&
				lazyTask.Value.IsCompletedSuccessfully)
			{
				var sensorData = lazyTask.Value.Result;
				sensorData.Data.Add((value, timestamp));
			}
		}

		private async Task TryEnsureSensorInitializedAsync(string topicPath, CancellationToken cancellationToken = default)
		{
			try
			{
				var lazyTask = _sensorDataTasks.GetOrAdd(topicPath, new Lazy<Task<SensorDataDto>>(() => CreateSensorAsync(topicPath, cancellationToken)));

				// Не ждем - просто обрабатываем исключения
				await lazyTask.Value;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Background initialization failed for {topicPath}");
			}
		}

		// Этот метод выполняется в фоне
		private async Task<SensorDataDto> CreateSensorAsync(string topicPath, CancellationToken cancellationToken = default)
		{
			try
			{
				var topicInfoResponse = await _rpcClient.SendRequestAsync<GetTopicInfoRequest, GetTopicInfoResponse>(
					new GetTopicInfoRequest(topicPath),
					"GetTopicInfo",
					TimeSpan.FromSeconds(30),
					cancellationToken
				);

				if (!topicInfoResponse.Success)
					throw new InvalidOperationException($"Failed to create sensor data for {topicPath}. Details {topicInfoResponse.ErrorMessage}.");

				var sensorData = new SensorDataDto
				{
					TopicPath = topicInfoResponse.TopicPath,
					Coordinate = new Coordinate(topicInfoResponse.Latitude, topicInfoResponse.Longitude),
					Altitude = topicInfoResponse.Altitude
				};

				// Переносим накопленные данные из временного хранилища
				if (_pendingData.TryRemove(topicPath, out var pendingData))
					sensorData.Data.AddRange(pendingData);

				return sensorData;
			}
			catch (Exception ex)
			{
				// Логируем ошибку, но не пробрасываем выше, т.к. это фоновая задача
				_logger.LogError(ex, $"Failed to create sensor data for {topicPath}");

				// Можно также оставить данные в _pendingData для повторной попытки
				return new SensorDataDto { TopicPath = "deleted" };
			}
		}
		// лучше один раз при запуске воркера (в StartAsync) запросить данные как GetTopics и записать их здесь в память!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	}
}
