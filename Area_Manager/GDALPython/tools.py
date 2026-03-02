from datetime import datetime

# Функция для записи логов в файл
def write_log(message):
    log_file = "/app/GDALPython/tmp/gdal_interface.log"  # Укажите путь к файлу логов
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")  # Формат: Год-Месяц-День Час:Минута:Секунда
    log_message = f"[{timestamp}] {message}"  # Добавляем время и дату к сообщению
    with open(log_file, 'a') as f:
        f.write(log_message + '\n')
        