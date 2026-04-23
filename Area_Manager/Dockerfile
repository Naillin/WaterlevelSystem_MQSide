# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WaterlevelSystem_MQSide/Area_Manager/Area_Manager.csproj", "WaterlevelSystem_MQSide/Area_Manager/"]
COPY ["WaterlevelSystem_MQSide/RabbitMQManager/RabbitMQManager.csproj", "WaterlevelSystem_MQSide/RabbitMQManager/"]
COPY ["WaterlevelSystem_MQSide/Contracts/Contracts.csproj", "WaterlevelSystem_MQSide/Contracts/"]

RUN dotnet restore "WaterlevelSystem_MQSide/Area_Manager/Area_Manager.csproj"

COPY WaterlevelSystem_MQSide/Area_Manager/ ./WaterlevelSystem_MQSide/Area_Manager/
COPY WaterlevelSystem_MQSide/RabbitMQManager/ ./WaterlevelSystem_MQSide/RabbitMQManager/
COPY WaterlevelSystem_MQSide/Contracts/ ./WaterlevelSystem_MQSide/Contracts/

WORKDIR "/src/WaterlevelSystem_MQSide/Area_Manager"
RUN dotnet build "./Area_Manager.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Area_Manager.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ===========================
# Финальный образ (Ubuntu 24.04)
# ===========================
FROM ghcr.io/osgeo/gdal:ubuntu-small-3.10.0 AS final
WORKDIR /app

# Устанавливаем нужные пакеты
RUN apt-get update && \
    apt-get install -y \
        dotnet-runtime-8.0 \
        python3-venv \
        python3-pip \
        libspatialindex-dev && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Исправляем конфликт версий PROJ
ENV PROJ_LIB=/usr/local/share/proj

# Создаем директорию для данных и временных файлов Python (docker run -d -v /путь/на/компьютере:/data area_manager_image)
RUN mkdir -p /data && \
    mkdir -p /app/GDALPython/tmp && \
    chmod -R 777 /app/GDALPython/tmp && \
    chmod -R 777 /app/GDALPython

# Копируем опубликованное .NET приложение
COPY --from=publish /app/publish .

# Копируем Python-скрипты
COPY --from=build /src/WaterlevelSystem_MQSide/Area_Manager/GDALPython/ ./GDALPython/

# Устанавливаем Python-зависимости в виртуальное окружение
RUN python3 -m venv /app/GDALPython/venv && \
    /app/GDALPython/venv/bin/pip install --upgrade pip && \
    /app/GDALPython/venv/bin/pip install -r /app/GDALPython/requirements.txt

# Указываем переменные окружения GDAL (на всякий случай)
ENV GDAL_DATA=/usr/share/gdal
ENV PROJ_LIB=/usr/share/proj

# Запускаем от имени непривилегированного пользователя
RUN useradd -m appuser
USER appuser

ENTRYPOINT ["dotnet", "Area_Manager.dll"]
