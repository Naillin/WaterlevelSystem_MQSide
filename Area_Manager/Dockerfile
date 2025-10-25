# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Area_Manager/Area_Manager.csproj", "Area_Manager/"]
COPY ["RabbitMQManager/RabbitMQManager.csproj", "RabbitMQManager/"]
RUN dotnet restore "./Area_Manager/Area_Manager.csproj"
COPY . .
WORKDIR "/src/Area_Manager"
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
        python3-pip && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Создаем директорию для данных и временных файлов Python
RUN mkdir -p /data && mkdir -p /app/GDALPython/tmp

# Копируем опубликованное .NET приложение
COPY --from=publish /app/publish .

# Копируем Python-скрипты
COPY --from=build /src/Area_Manager/GDALPython/ ./GDALPython/

# Устанавливаем Python-зависимости в виртуальное окружение
RUN python3 -m venv /app/GDALPython/venv && \
    /app/GDALPython/venv/bin/pip install --upgrade pip && \
    /app/GDALPython/venv/bin/pip install -r /app/GDALPython/requirements.txt

# Создаем FIFO файлы
RUN mkfifo /app/GDALPython/tmp/python_to_csharp && \
    mkfifo /app/GDALPython/tmp/csharp_to_python

# Устанавливаем права на FIFO директорию и файлы
RUN chmod -R 777 /app/GDALPython/tmp && \
    chmod 666 /app/GDALPython/tmp/python_to_csharp && \
    chmod 666 /app/GDALPython/tmp/csharp_to_python

# Указываем переменные окружения GDAL (на всякий случай)
ENV GDAL_DATA=/usr/share/gdal
ENV PROJ_LIB=/usr/share/proj

# Запускаем от имени непривилегированного пользователя
RUN useradd -m appuser
USER appuser

ENTRYPOINT ["dotnet", "Area_Manager.dll"]
