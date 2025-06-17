using System.Text.Encodings.Web;
using System.Text.Json;

namespace tournamentApp.AuditService
{
	public class FileLogger
	{
		private readonly string _logFolderPath;

		private readonly string _logFilePath;

		public FileLogger(IWebHostEnvironment env)
		{
			_logFolderPath = Path.Combine(env.ContentRootPath, "Logs");

			_logFilePath = Path.Combine(_logFolderPath, "actions.log");
		}

		public void AddLogToFile(IWebHostEnvironment env, string action, int? userId, string details)
		{
			if (!Directory.Exists(env.ContentRootPath + "Logs"))
			{
				Directory.CreateDirectory(_logFolderPath);
			}

			var logEntry = new
			{
				Timestamp = DateTime.Now,
				Action = action,
				UserId = userId,
				Details = details
			};

			var options = new JsonSerializerOptions
			{
				WriteIndented = true,
				Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
			};

			Task.Run(() =>
			{
				try
				{
					File.AppendAllText(_logFilePath,
						JsonSerializer.Serialize(logEntry, options) + Environment.NewLine);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Ошибка записи лога: {ex.Message}");
				}
			});
		}
	}
}
