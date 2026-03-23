using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FleetManagement.Desktop.Services
{
	public static class DatabaseBackupService
	{
		public static string CreateBackup(
			string pgDumpPath,
			string host,
			int port,
			string database,
			string username,
			string password)
		{
			AppPaths.EnsureFolders();

			var backupFolder = AppPaths.BackupsFolder;

			var backupFile = Path.Combine(
				backupFolder,
				$"{database}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.backup");

			var psi = new ProcessStartInfo
			{
				FileName = pgDumpPath,
				Arguments = $"-h {host} -p {port} -U {username} -F c -b -v -f \"{backupFile}\" \"{database}\"",
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			};

			psi.Environment["PGPASSWORD"] = password;

			using var process = new Process { StartInfo = psi };
			process.Start();

			var stdOut = process.StandardOutput.ReadToEnd();
			var stdErr = process.StandardError.ReadToEnd();

			process.WaitForExit();

			if (process.ExitCode != 0)
			{
				throw new Exception("Veritabanı yedekleme başarısız.\n" + stdErr);
			}

			CleanOldBackups(backupFolder);

			AppLogger.Info("DatabaseBackupService.CreateBackup", "Yedek alma işlemi tamamlandı.");

			return backupFile;
		}

		public static void CleanOldBackups(string backupFolder, int maxFiles = 5)
		{
			try
			{
				if (!Directory.Exists(backupFolder))
					return;

				var files = new DirectoryInfo(backupFolder)
					.GetFiles("*.backup")
					.OrderByDescending(f => f.CreationTime)
					.ToList();

				if (files.Count <= maxFiles)
					return;

				foreach (var file in files.Skip(maxFiles))
				{
					file.Delete();
				}
			}
			catch
			{
			}
		}
	}
}