using Microsoft.Extensions.DependencyInjection;
using MQGateway.Core.Interfaces;
using MQGateway.Core.Models.GetTopicInfo;
using RabbitMQManager.Core.Attributes;
using RabbitMQManager.Implementations.RabbitMQ.RPC.Strategies;

namespace MQGateway.Strategies
{
	[Command("GetTopicInfo")]
	internal class GetTopicInfo : MQStrategy<GetTopicInfoRequest, GetTopicInfoResponse>
	{
		private readonly IServiceScopeFactory _scopeFactory;

		public GetTopicInfo(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

		protected override async Task<GetTopicInfoResponse> Handle(GetTopicInfoRequest request, CancellationToken cancellationToken = default)
		{
			GetTopicInfoResponse? response = null;

			using (var scope = _scopeFactory.CreateScope())
			{
				var dataRepository = scope.ServiceProvider.GetRequiredService<IDataRepository>();

				if (string.IsNullOrWhiteSpace(request.topicPath))
					throw new InvalidOperationException($"topicPath is null.");

				var topic = await dataRepository.GetTopicAsync(request.topicPath, cancellationToken);

				if (topic == null)
					response = new GetTopicInfoResponse
					{
						RequestId = request.RequestId!,
						Type = "GetTopicInfo",
						Success = false,
						ErrorMessage = $"There is no topic with path {request.topicPath}."
					};
				else
					response = new GetTopicInfoResponse
					{
						RequestId = request.RequestId!,
						Type = "GetTopicInfo",
						Success = true,
						ErrorMessage = string.Empty,
						Latitude = topic.Latitude_Topic,
						Longitude = topic.Longitude_Topic,
						Altitude = topic.Altitude_Topic
					};
			}

			return response;
		}
	}
}
