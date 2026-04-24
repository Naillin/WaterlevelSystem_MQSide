using Contracts.Models;
using Contracts.Models.RabbitMQ.RPC.GetAllTopicsWithData;
using Microsoft.Extensions.DependencyInjection;
using MQGateway.Core.Interfaces;
using RabbitMQManager.Core.Attributes;
using RabbitMQManager.Implementations.RabbitMQ.RPC.Strategies;

namespace MQGateway.Strategies;

[Command("GetAllTopicsWithData")]
internal class GetAllTopicsWithData : MQStrategy<GetAllTopicsWithDataRequest, GetAllTopicsWithDataResponse>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public GetAllTopicsWithData(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    protected override async Task<GetAllTopicsWithDataResponse> Handle(GetAllTopicsWithDataRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            GetAllTopicsWithDataResponse? response = null;

            using (var scope = _scopeFactory.CreateScope())
            {
                var dataRepository = scope.ServiceProvider.GetRequiredService<IDataRepository>();

                var topics = await dataRepository.GetTopicsAsync(cancellationToken);
                var tasks = topics.Select(async topic =>
                {
                    return new SensorDataDto
                    {
                        TopicPath = topic.Path_Topic!,
                        Coordinate = new Coordinate(topic.Latitude_Topic, topic.Longitude_Topic),
                        Altitude = topic.Altitude_Topic,
                        Data = await GetTopicData(topic.ID_Topic, dataRepository, cancellationToken)
                    };
                });
                List<SensorDataDto> topicsData = (await Task.WhenAll(tasks)).ToList();

                response = new GetAllTopicsWithDataResponse
                {
                    RequestId = request.RequestId!,
                    Type = "GetAllTopicsWithData",
                    Success = true,
                    ErrorMessage = string.Empty,
                    Topics = topicsData
                };
            }

            return response;
        }
        catch (Exception exception)
        {
            return new GetAllTopicsWithDataResponse
            {
                RequestId = request.RequestId!,
                Type = "GetAllTopicsWithData",
                Success = false,
                ErrorMessage = exception.Message,
                Topics = new List<SensorDataDto>()
            };
        }
    }

    private async Task<List<ValueAtTime>> GetTopicData(int topicId, IDataRepository dataRepository, CancellationToken cancellationToken = default)
    {
        var dataPack = await dataRepository.GetDataAsync(topicId, cancellationToken);

        return dataPack
            .Select(data => new ValueAtTime(double.Parse(data.Value_Data), data.Time_Data))
            .ToList();
    }
}