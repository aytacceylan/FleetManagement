using System;
using System.IO;

namespace FleetManagement.Desktop.Services
{
	public static class AppPaths
	{
		public static string BaseFolder =>
			Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				"OtoSevk");

		public static string LogsFolder =>
			Path.Combine(BaseFolder, "Logs");

		public static string BackupsFolder =>
			Path.Combine(BaseFolder, "Backups");

		public static void EnsureFolders()
		{
			Directory.CreateDirectory(BaseFolder);
			Directory.CreateDirectory(LogsFolder);
			Directory.CreateDirectory(BackupsFolder);
		}
	}
}