using Microsoft.Extensions.DependencyInjection;
using MQGateway.Core.Interfaces;
using MQGateway.Core.Models.GetAllTopics;
using RabbitMQManager.Core.Attributes;
using RabbitMQManager.Implementations.RabbitMQ.RPC.Strategies;

namespace MQGateway.Strategies
{
	[Command("GetAllTopicsRequest")]
	internal class GetTopics : MQStrategy<GetAllTopicsRequest, GetAllTopicsResponse>
	{
		private readonly IServiceScopeFactory _scopeFactory;

		public GetTopics(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

		protected override async Task<GetAllTopicsResponse> Handle(GetAllTopicsRequest request, CancellationToken cancellationToken = default)
		{
			GetAllTopicsResponse? response = null;

			using (var scope = _scopeFactory.CreateScope())
			{
				var dataRepository = scope.ServiceProvider.GetRequiredService<IDataRepository>();

				var topics = await dataRepository.GetTopicsAsync();
				List<string> paths = topics
					.Select(t => t.Path_Topic)
					.OfType<string>()
					.ToList();

				response = new GetAllTopicsResponse
				{
					RequestId = request.RequestId!,
					Type = "GetAllTopicsRequest",
					Success = true,
					ErrorMessage = string.Empty,
					Topics = paths,
				};
			}

			return response;
		}
	}
}
