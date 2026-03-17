using System;
using System.Diagnostics;
using System.IO;

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
            // var backupFolder = @"C:\Oto Sevk Programı\1.Yedekler";
            var backupFolder = Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "OtoSevk",
                        "Backups");

            Directory.CreateDirectory(backupFolder);

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

            AppLogger.Info("DatabaseBackupService.CreateBackup", $"Yedek alındı: {backupFile}");

            return backupFile;
        }
    }
}