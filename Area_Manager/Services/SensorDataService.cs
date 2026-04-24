using Area_Manager.Core.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQManager.Core.Interfaces.MQ.RPC;
using System.Collections.Concurrent;
using Contracts.Models;
using Contracts.Models.RabbitMQ;
using Contracts.Models.RabbitMQ.RPC.GetTopicInfo;

namespace Area_Manager.Services;

internal class SensorDataService : ISensorDataService
{
	private readonly ILogger<SensorDataService> _logger;
	private readonly IRPC_Client _rpcClient;
	private readonly ISensorCacheService _sensorCacheService;

	private readonly ConcurrentDictionary<string, Lazy<Task<SensorDataDto>>> _sensorDataTasks = new();
	private readonly ConcurrentDictionary<string, List<ValueAtTime>> _pendingData = new();

	public SensorDataService(IRPC_Client rpcClient, ISensorCacheService sensorCacheService, ILogger<SensorDataService> logger)
	{
		_rpcClient = rpcClient;
		_sensorCacheService = sensorCacheService;

		_logger = logger;
	}

	public async Task LoadData(CancellationToken cancellationToken = default)
	{
		_sensorDataTasks.Clear();

		var sensorCache = await _sensorCacheService.GetAllSensorsWithData(cancellationToken);
		if (sensorCache is null)
			return;

		foreach (var sensor in sensorCache)
		{
			// Оборачиваем готовый объект в завершенную задачу
			var completedTask = Task.FromResult(sensor);

			// Оборачиваем задачу в Lazy, которая сразу же "вычислена"
			var lazyTask = new Lazy<Task<SensorDataDto>>(() => completedTask);

			// Принудительно обращаемся к Value, чтобы IsValueCreated стало true
			var _ = lazyTask.Value; 

			_sensorDataTasks.TryAdd(sensor.TopicPath, lazyTask);
		}
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
			// todo: Можно также добавить логику для сенсоров, которые еще в процессе создания
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
			_logger.LogInformation($"Adding data to {topicPath}.");
			AddDataToExistingSensor(topicPath, sensorEvent.Value, sensorEvent.Date);
			return Task.CompletedTask;
		}

		// МЕДЛЕННЫЙ ПУТЬ: сенсор еще не готов
		_logger.LogInformation($"Unregistered sensor in the system - {topicPath}. Adding data to temporary storage.");
		AddDataToPending(topicPath, sensorEvent.Value, sensorEvent.Date, cancellationToken);

		return Task.CompletedTask;
	}

	private bool IsSensorFullyCreated(string topicPath) =>
		_sensorDataTasks.TryGetValue(topicPath, out var lazyTask) &&
		lazyTask.IsValueCreated &&
		lazyTask.Value.IsCompletedSuccessfully;

	private void AddDataToExistingSensor(string topicPath, double value, DateTimeOffset dateTime)
	{
		if (_sensorDataTasks.TryGetValue(topicPath, out var lazyTask) &&
		    lazyTask.IsValueCreated &&
		    lazyTask.Value.IsCompletedSuccessfully)
		{
			var sensorData = lazyTask.Value.Result;
			sensorData.Data.Add(new ValueAtTime(value, dateTime));
		}
	}
	
	private void AddDataToPending(string topicPath, double value, DateTimeOffset dateTime, CancellationToken cancellationToken = default)
	{
		bool isNewTopic = false;

		_pendingData.AddOrUpdate(topicPath,
			// Если топика еще нет в словаре:
			(key) => 
			{
				isNewTopic = true; // Фиксируем, что мы инициаторы
				return new List<ValueAtTime> { new ValueAtTime(value, dateTime) };
			},
			// Если топик уже есть (инициализация в процессе):
			(key, list) => 
			{
				lock (list) // Защита обычного List от параллельной записи
				{
					list.Add(new ValueAtTime(value, dateTime));
				}
				return list;
			});

		// Запускаем инициализацию ТОЛЬКО если мы создали запись первыми
		if (isNewTopic)
		{
			_logger.LogInformation($"Attempting to register - {topicPath}.");
			// Запускаем фоновую инициализацию fire-and-forget
			_ = Task.Run(() => TryEnsureSensorInitializedAsync(topicPath, cancellationToken));
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
			_logger.LogError(ex, $"Background initialization failed for {topicPath}.");
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
			{
				_logger.LogWarning($"Topic {topicPath} is invalid in DB. Stopping attempts. Error: {topicInfoResponse.ErrorMessage}");
            
				// Очищаем накопленные данные, чтобы не жрать память
				_pendingData.TryRemove(topicPath, out _); 
            
				// Возвращаем "заглушку". Теперь IsSensorFullyCreated будет true, 
				// но мы будем знать, что это "битый" топик.
				return new SensorDataDto { TopicPath = "INVALID_TOPIC" };
			}
				
			var sensorData = new SensorDataDto
			{
				TopicPath = topicInfoResponse.TopicPath,
				Coordinate = new Coordinate(topicInfoResponse.Latitude, topicInfoResponse.Longitude),
				Altitude = topicInfoResponse.Altitude
			};

			// Переносим накопленные данные из временного хранилища
			if (_pendingData.TryRemove(topicPath, out var pendingData))
				lock (pendingData) 
					sensorData.Data.AddRange(pendingData);

			return sensorData;
		}
		catch (Exception ex)
		{
			// Логируем ошибку, но не пробрасываем выше, т.к. это фоновая задача
			_logger.LogError(ex, $"Registration error for sensor - {topicPath}.");
				
			// Удаляем себя из словаря, чтобы следующая попытка AddData 
			// снова вызвала CreateSensorAsync, а не вернула этот упавший Task.
			_sensorDataTasks.TryRemove(topicPath, out _);
        
			throw; // Пробрасываем ошибку дальше в Task
		}
	}
		
	public void DeleteSensorsAsync(IList<string> topicKeys)
	{
		foreach (var key in topicKeys)
		{
			_sensorDataTasks.TryRemove(key, out _);
			_logger.LogInformation($"Sensor {key} is deleted.");
		}
	}
}