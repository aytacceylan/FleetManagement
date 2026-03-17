using System;
using System.IO;
using System.Text;

namespace FleetManagement.Desktop.Services
{
    public static class AppLogger
    {
        // log kayıt yeri / path
        private static readonly string LogFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OtoSevk", "Logs");

        private static readonly string LogFile =
            Path.Combine(LogFolder, $"system_{DateTime.Now:yyyy-MM-dd}.log");

        public static void Info(string source, string message)
        {
            Write("INFO", source, message, null);
        }

        public static void Error(string source, string message, Exception? ex = null)
        {
            Write("ERROR", source, message, ex);
        }

        private static void Write(string level, string source, string message, Exception? ex)
        {
            try
            {
                Directory.CreateDirectory(LogFolder);

                var sb = new StringBuilder();
                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine($"Tarih   : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Seviye  : {level}");
                sb.AppendLine($"Kaynak  : {source}");
                sb.AppendLine($"Mesaj   : {message}");

                if (ex != null)
                {
                    sb.AppendLine("Hata    : " + ex.Message);
                    sb.AppendLine("Detay   : " + ex);
                }

                File.AppendAllText(LogFile, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                // Log yazımı hata verirse uygulamayı durdurmasın
            }
        }
    }
}