using System;
using System.IO;

namespace FleetManagement.Desktop.Services
{
	public static class AppLogger
	{
		private static string LogFolder => AppPaths.LogsFolder;

		private static string LogFile =>
			Path.Combine(LogFolder, $"log_{DateTime.Now:yyyy-MM-dd}.txt");

		public static void Info(string source, string message)
		{
			Write("INFO", source, message, null);
		}

		public static void Error(string source, string message, Exception? ex = null)
		{
			Write("ERROR", source, message, ex);
		}

		public static void CleanOldLogs(int days = 30)
		{
			try
			{
				if (!Directory.Exists(LogFolder))
					return;

				var files = Directory.GetFiles(LogFolder, "*.txt");

				foreach (var file in files)
				{
					var info = new FileInfo(file);

					if (info.CreationTime < DateTime.Now.AddDays(-days))
						info.Delete();
				}
			}
			catch
			{
			}
		}

		private static void Write(string level, string source, string message, Exception? ex)
		{
			try
			{
				AppPaths.EnsureFolders();

				var log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{source}] {message}";

				if (ex != null)
					log += $"\nEXCEPTION: {ex}";

				File.AppendAllText(LogFile, log + "\n\n");
			}
			catch
			{
			}
		}
	}
}