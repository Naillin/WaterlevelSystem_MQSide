# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WaterlevelSystem_MQSide/MQGateway/MQGateway.csproj", "WaterlevelSystem_MQSide/MQGateway/"]
COPY ["WaterlevelSystem_MQSide/RabbitMQManager/RabbitMQManager.csproj", "WaterlevelSystem_MQSide/RabbitMQManager/"]
COPY ["WaterlevelSystem_MQSide/Contracts/Contracts.csproj", "WaterlevelSystem_MQSide/Contracts/"]
COPY ["WaterlevelSystem_DataBaseStructure/WaterlevelSystem_DataBaseStructure/WaterlevelSystem_DataBaseStructure.csproj", "WaterlevelSystem_DataBaseStructure/WaterlevelSystem_DataBaseStructure/"]

RUN dotnet restore "WaterlevelSystem_MQSide/MQGateway/MQGateway.csproj"

COPY WaterlevelSystem_MQSide/MQGateway/ ./WaterlevelSystem_MQSide/MQGateway/
COPY WaterlevelSystem_MQSide/RabbitMQManager/ ./WaterlevelSystem_MQSide/RabbitMQManager/
COPY WaterlevelSystem_MQSide/Contracts/ ./WaterlevelSystem_MQSide/Contracts/
COPY WaterlevelSystem_DataBaseStructure/WaterlevelSystem_DataBaseStructure/ ./WaterlevelSystem_DataBaseStructure/WaterlevelSystem_DataBaseStructure/

WORKDIR "/src/WaterlevelSystem_MQSide/MQGateway"
RUN dotnet build "./MQGateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MQGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MQGateway.dll"]
