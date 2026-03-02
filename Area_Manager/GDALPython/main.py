import os
from gdal_tile_interface import GDALTileInterface
from tools import write_log

# Пути к FIFO-файлам
fifo_to_python = "/app/GDALPython/tmp/csharp_to_python"
fifo_from_python = "/app/GDALPython/tmp/python_to_csharp"

# Создание FIFO, если они не существуют
if not os.path.exists(fifo_to_python):
    os.mkfifo(fifo_to_python)
if not os.path.exists(fifo_from_python):
    os.mkfifo(fifo_from_python)

def main():
    # Инициализация интерфейса
    tiles_folder = '/data'
    summary_file = '/data/summaryFile.json'
    tile_interface = GDALTileInterface(tiles_folder, summary_file)

    if not tile_interface.has_summary_json():
        tile_interface.create_summary_json()
    else:
        tile_interface.read_summary_json()

    # Открываем FIFO один раз
    with open(fifo_to_python, 'r') as fifo_in, open(fifo_from_python, 'w') as fifo_out:
        try:
            while True:
                line = fifo_in.readline().strip()
                if not line:
                    continue  # ждем новые данные, не выходим

                if line == "EXIT":
                    write_log("Exiting...")
                    break

                if line == "hello_from_csharp":
                    write_log("HealthCheck received: hello from csharp")
                    fifo_out.write("hello_from_python\n")
                    fifo_out.flush()
                    continue

                try:
                    lat, lon = map(float, line.split(','))
                    height = tile_interface.lookup(lat, lon)
                    if height is None:
                        write_log(f'Height not found for coordinates: ({lat}, {lon})')
                        result = "NULL"
                    else:
                        write_log(f'Height for coordinates: ({lat}, {lon}) = {height}')
                        result = str(height)
                except Exception as e:
                    write_log(f'Error processing coordinates input "{line}" - {e}')
                    result = f"ERROR: {e}"

                # пишем в FIFO (и сразу flush, чтобы читатель получил данные)
                fifo_out.write(result + '\n')
                fifo_out.flush()

        finally:
            tile_interface.close_all_interfaces()
            write_log("Python process resources cleaned up.")

if __name__ == "__main__":
    main()
