using System;
using Microsoft.Extensions.Configuration;

namespace FleetManagement.Desktop.Services
{
    public static class ConnectionStringParser
    {
        public static ConnectionStringInfo ParseFleetDb()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var cs = config.GetConnectionString("FleetDb")
                     ?? throw new InvalidOperationException("ConnectionStrings:FleetDb bulunamadı.");

            var parts = cs.Split(';', StringSplitOptions.RemoveEmptyEntries);

            var info = new ConnectionStringInfo();

            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length != 2) continue;

                var key = kv[0].Trim().ToLowerInvariant();
                var value = kv[1].Trim();

                switch (key)
                {
                    case "host":
                        info.Host = value;
                        break;
                    case "port":
                        if (int.TryParse(value, out var p))
                            info.Port = p;
                        break;
                    case "database":
                        info.Database = value;
                        break;
                    case "username":
                    case "user id":
                    case "user":
                        info.Username = value;
                        break;
                    case "password":
                        info.Password = value;
                        break;
                }
            }

            return info;
        }
    }
}