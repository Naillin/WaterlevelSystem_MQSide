using Area_Manager.Core;
using System.Diagnostics;
using System.Globalization;

namespace Area_Manager.GDALPython
{
	internal class GDALPython : IDisposable
	{
		private string _pythonPath = "/app/GDALPython/venv/bin/python3";
		private string _scriptPath = "/app/GDALPython/main.py";
		private string _fifoToPython = "/app/GDALPython/tmp/csharp_to_python";  // FIFO для отправки данных в Python
		private string _fifoFromPython = "/app/GDALPython/tmp/python_to_csharp";  // FIFO для получения данных из Python

		//private readonly ILogger<GDALPython> _logger;

		private Process? _pythonProcess;
		private StreamWriter? _writer;
		private StreamReader? _reader;

		private int debugWrite = 200;
		private static int debugWriteCheck = 0;

		public GDALPython(string pythonPath, string scriptPath/*, ILogger<GDALPython> logger*/)
		{
			_pythonPath = pythonPath;
			_scriptPath = scriptPath;

			//_logger = logger;

			StartPythonProcess();
		}

		public void StartPythonProcess()
		{
			// Запуск Python-скрипта
			_pythonProcess = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = _pythonPath,
					Arguments = _scriptPath,
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

			Console.WriteLine("Start Python.");
			_pythonProcess.Start();
			
			// Открываем FIFO один раз
			Console.WriteLine("Open writer.");
			_writer = new StreamWriter(_fifoToPython) { AutoFlush = true };
			Console.WriteLine("Open reader.");
			_reader = new StreamReader(_fifoFromPython);
			Console.WriteLine("Python started.");
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
						Console.WriteLine($"Высота точки {coordinate.Latitude}, {coordinate.Longitude}: {result}.");
						debugWriteCheck = 0;
					}
					debugWriteCheck++;
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
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in disposing! Details: {ex.ToString()}");
			}
			finally
			{
				Console.WriteLine("Python stoped.");
			}
		}
	}
}
