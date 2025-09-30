using Area_Manager.Core;
using System.Diagnostics;
using System.Globalization;

namespace Area_Manager.GDALPython
{
	internal class GDALPython : IDisposable
	{
		private string _pythonPath = "GDALPython/venv/bin/python3";
		private string _scriptPath = "GDALPython/main.py";
		private string _fifoToPython = "GDALPython/tmp/csharp_to_python";  // FIFO для отправки данных в Python
		private string _fifoFromPython = "GDALPython/tmp/python_to_csharp";  // FIFO для получения данных из Python

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
					UseShellExecute = false,
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				}
			};

			Console.WriteLine("Start Python.");
			_pythonProcess.Start();
			// Открываем FIFO один раз
			Console.WriteLine("Open writer.");
			_writer = new StreamWriter(_fifoToPython) { AutoFlush = true };
			Console.WriteLine("Open reader.");
			_reader = new StreamReader(_fifoFromPython);
			Console.WriteLine("Python started.");
		}

		public double GetElevation(Coordinate coordinate)
		{
			double result = -32768;

			//Console.WriteLine("GetElevation method from Python.");

			try
			{
				// Отправка координат в Python через FIFO
				string coordinates = $"{coordinate.Latitude.ToString(CultureInfo.InvariantCulture)},{coordinate.Longitude.ToString(CultureInfo.InvariantCulture)}";
				_writer!.WriteLine(coordinates);

				// Чтение результата из FIFO
				string? resultStr = _reader!.ReadLine();
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
