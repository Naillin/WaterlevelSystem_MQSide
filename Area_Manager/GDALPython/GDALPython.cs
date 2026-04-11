using Area_Manager.Core;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Area_Manager.Core.Interfaces;

namespace Area_Manager.GDALPython
{
	internal class GDALPython : IGDALPython, IDisposable
	{
		[DllImport("libc", SetLastError = true, CharSet = CharSet.Ansi)]
		private static extern int mkfifo(string path, uint mode);
		
		private string _pythonPath = "/app/GDALPython/venv/bin/python3";
		private string _scriptPath = "/app/GDALPython/main.py";
		private string _fifoToPython = "/app/GDALPython/tmp/csharp_to_python";  // FIFO для отправки данных в Python
		private string _fifoFromPython = "/app/GDALPython/tmp/python_to_csharp";  // FIFO для получения данных из Python
		private string _guid;

		//private readonly ILogger<GDALPython> _logger;

		private Process? _pythonProcess;
		private StreamWriter? _writer;
		private StreamReader? _reader;

		private int debugWrite = 500;
		private static int debugWriteCheck = 0;
		private long pointNumber = 0;

		public GDALPython(string pythonPath, string scriptPath/*, ILogger<GDALPython> logger*/)
		{
			_pythonPath = pythonPath;
			_scriptPath = scriptPath;

			_guid = Guid.NewGuid().ToString("N");
			_fifoToPython = $"/app/GDALPython/tmp/csharp_to_python_{_guid}.fifo";
			_fifoFromPython = $"/app/GDALPython/tmp/python_to_csharp_{_guid}.fifo";
			CreateFifo(_fifoToPython);
			CreateFifo(_fifoFromPython);
			
			//_logger = logger;

			StartPythonProcess();
		}
		
		private void CreateFifo(string path)
		{
			Console.WriteLine($"Сreating fifo {path}");
			// 0666 (десятичное 438) — чтение/запись для всех
			int res = mkfifo(path, 438);
			if (res != 0)
			{
				int error = Marshal.GetLastWin32Error();
				// Ошибка 17 (EEXIST) — файл уже есть, это нормально
				if (error != 17) 
					throw new Exception($"Не удалось создать FIFO {path}. Код ошибки: {error}");
			}
			
			Console.WriteLine($"Fifo created: {path}");
		}

		public void StartPythonProcess()
		{
			string arguments = $"\"{_scriptPath}\" --input \"{_fifoToPython}\" --output \"{_fifoFromPython}\"";
			
			// Запуск Python-скрипта
			_pythonProcess = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = _pythonPath,
					Arguments = arguments,
					WorkingDirectory = "/app/GDALPython",
					UseShellExecute = false,
					RedirectStandardInput = false,
					RedirectStandardOutput = false,
					RedirectStandardError = false,
					CreateNoWindow = true
				}
			};
			_pythonProcess.StartInfo.EnvironmentVariables["PROJ_LIB"] = "/usr/local/share/proj";
			_pythonProcess.StartInfo.EnvironmentVariables["PYTHONPATH"] = "/app/GDALPython";

			Console.WriteLine($"Start Python - {_guid}");
			_pythonProcess.Start();
			
			// Открываем FIFO один раз
			Console.WriteLine("Open writer");
			_writer = new StreamWriter(_fifoToPython) { AutoFlush = true };
			Console.WriteLine("Open reader");
			_reader = new StreamReader(_fifoFromPython);
			Console.WriteLine("Python started");
		}

		public bool HealthCheck()
		{
			try
			{
				Console.WriteLine("Performing HealthCheck...");
				_writer!.WriteLine("hello_from_csharp");
        
				// Ждем ответа (можно добавить Timeout для надежности)
				string? response = _reader!.ReadLine();
        
				if (response == "hello_from_python")
				{
					Console.WriteLine("HealthCheck passed!");
					return true;
				}
				
				Console.WriteLine($"HealthCheck failed. Unexpected response: {response}");
				return false;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"HealthCheck error: {ex.Message}");
				return false;
			}
		}
		
		public async Task<double> GetElevation(Coordinate coordinate, CancellationToken cancellationToken = default)
		{
			double result = -32768;

			//Console.WriteLine("GetElevation method from Python.");

			try
			{
				// Отправка координат в Python через FIFO
				string coordinates = $"{coordinate.Latitude.ToString(CultureInfo.InvariantCulture)},{coordinate.Longitude.ToString(CultureInfo.InvariantCulture)}";
				await _writer!.WriteLineAsync(coordinates.AsMemory(), cancellationToken); // проверить передачу

				// Чтение результата из FIFO
				string? resultStr = await _reader!.ReadLineAsync(cancellationToken);
				if (resultStr == null || resultStr == "NULL")
				{
					result = -32768;
				}
				else if (resultStr.StartsWith("ERROR:"))
				{
					result = -32768;
				}
				else
				{
					result = Convert.ToDouble(resultStr);
					if (debugWriteCheck >= debugWrite)
					{
						Console.WriteLine($"Высота точки №{pointNumber} {coordinate.Latitude}, {coordinate.Longitude}: {result}");
						debugWriteCheck = 0;
					}
					debugWriteCheck++;
					pointNumber++;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in GetElevation! Точка {coordinate.Latitude}, {coordinate.Longitude}. Details: {ex.ToString()}");
			}

			return result;
		}

		public void Dispose()
		{
			try
			{
				_writer?.WriteLine("EXIT");
				_writer?.Dispose();
				_reader?.Dispose();

				if (!_pythonProcess!.HasExited)
					_pythonProcess.Kill();
				
				if (File.Exists(_fifoToPython)) File.Delete(_fifoToPython);
				if (File.Exists(_fifoFromPython)) File.Delete(_fifoFromPython);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in disposing GDALPython - {_guid}! Details: {ex.ToString()}");
			}
			finally
			{
				Console.WriteLine($"Exited from GDALPython - {_guid}");
			}
		}
	}
}
