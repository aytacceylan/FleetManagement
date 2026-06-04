using System;
using System.IO;

namespace FleetManagement.Desktop.Services
{
    public static class AppPaths
    {
        public static string RootFolder =>
            Directory.Exists(@"D:\")
                ? @"D:\OtoSevk"
                : Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "OtoSevk");

        public static string LogsFolder =>
            Path.Combine(RootFolder, "Logs");

        public static string BackupsFolder =>
            Path.Combine(RootFolder, "Backups");

        public static string ReportsFolder =>
            Path.Combine(RootFolder, "Reports");

        public static string TaskRegistersFolder =>
            Path.Combine(RootFolder, "TaskRegisters");

        public static void EnsureFolders()
        {
            Directory.CreateDirectory(RootFolder);
            Directory.CreateDirectory(LogsFolder);
            Directory.CreateDirectory(BackupsFolder);
            Directory.CreateDirectory(ReportsFolder);
            Directory.CreateDirectory(TaskRegistersFolder);
        }
    }
}