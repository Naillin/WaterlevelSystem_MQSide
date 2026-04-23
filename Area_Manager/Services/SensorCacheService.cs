using Area_Manager.Core.Interfaces;
using Contracts.Models;
using Contracts.Models.RabbitMQ.RPC.GetAllTopicsWithData;
using Microsoft.Extensions.Logging;
using RabbitMQManager.Core.Interfaces.MQ.RPC;

namespace Area_Manager.Services;

public class SensorCacheService : ISensorCacheService
{
    private readonly ILogger<SensorCacheService> _logger;
    private readonly IRPC_Client _rpcClient;

    public SensorCacheService(IRPC_Client rpcClient, ILogger<SensorCacheService> logger)
    {
        _rpcClient = rpcClient;

        _logger = logger;
    }

    public async Task<IList<SensorDataDto>?> GetAllSensorsWithData(CancellationToken cancellationToken = default)
    {
        var topicDataResponse = await _rpcClient.SendRequestAsync<GetAllTopicsWithDataRequest, GetAllTopicsWithDataResponse>(
            new GetAllTopicsWithDataRequest(),
            "GetTopicInfo",
            TimeSpan.FromSeconds(30),
            cancellationToken
        );

        if (!topicDataResponse.Success)
        {
            _logger.LogWarning($"Load sensor cache failed. {topicDataResponse.ErrorMessage}");

            return null;
        }
        
        return topicDataResponse.Topics;
    }
}