using Microsoft.Extensions.Configuration;
using 施工定额.Entity;

namespace 施工定额.Helper
{
    public static class AppConfig
    {
        private static readonly IConfiguration _config;
        private static FeeRateSettings? _feeRates;

        /// <summary>%AppData%\施工定额</summary>
        public static string DataDirectory { get; }

        static AppConfig()
        {
            DataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "施工定额");
            Directory.CreateDirectory(DataDirectory);

            _config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            EnsureUserDatabase();
        }

        public static string UserDbConn => ResolveConn("UserDb", preferAppData: true);
        public static string SystemDbConn => ResolveConn("SystemDb", preferAppData: false);
        public static string UpdateVersionInfoUrl => _config["UpdateSettings:VersionInfoUrl"] ?? "";
        public static string AppUpdateVersionInfoUrl => _config["UpdateSettings:AppVersionInfoUrl"] ?? "";

        public static FeeRateSettings FeeRates
        {
            get
            {
                if (_feeRates != null)
                    return _feeRates;
                _feeRates = LoadFeeRates();
                return _feeRates;
            }
        }

        public static FeeRateSettings ReloadFeeRates()
        {
            _feeRates = LoadFeeRates();
            return _feeRates;
        }

        private static FeeRateSettings LoadFeeRates()
        {
            var settings = new FeeRateSettings();
            var section = _config.GetSection("FeeSettings");
            if (!section.Exists())
                return settings;

            settings.OverheadBase = section["OverheadBase"] ?? settings.OverheadBase;
            if (decimal.TryParse(section["OverheadRate"], out var overhead))
                settings.OverheadRate = overhead;
            if (decimal.TryParse(section["ProfitRate"], out var profit))
                settings.ProfitRate = profit;
            if (decimal.TryParse(section["StatutoryFeeRate"], out var statutory))
                settings.StatutoryFeeRate = statutory;
            if (decimal.TryParse(section["VatRate"], out var vat))
                settings.VatRate = vat;
            if (bool.TryParse(section["IncludeStatutoryInUnitPrice"], out var include))
                settings.IncludeStatutoryInUnitPrice = include;
            return settings;
        }

        public static string SystemDbFilePath => ExtractDataSource(SystemDbConn);
        public static string UserDbFilePath => ExtractDataSource(UserDbConn);

        private static string ExtractDataSource(string connStr)
        {
            var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = connStr };
            return builder["Data Source"]?.ToString() ?? "";
        }

        private static void EnsureUserDatabase()
        {
            var userPath = Path.Combine(DataDirectory, "userDB.db");
            if (File.Exists(userPath))
                return;

            var bundled = Path.Combine(AppContext.BaseDirectory, "userDB.db");
            try
            {
                if (File.Exists(bundled))
                    File.Copy(bundled, userPath);
            }
            catch (Exception ex)
            {
                AppLogger.Error("初始化用户数据库失败", ex);
            }
        }

        private static string ResolveConn(string key, bool preferAppData)
        {
            var raw = _config.GetConnectionString(key)
                ?? throw new InvalidOperationException($"配置文件中找不到连接字符串: {key}");

            var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = raw };

            if (builder.ContainsKey("Version"))
                builder.Remove("Version");

            if (!builder.TryGetValue("Data Source", out var dsObj))
                return builder.ConnectionString;

            var dataSource = Environment.ExpandEnvironmentVariables(dsObj?.ToString() ?? "");
            if (Path.IsPathRooted(dataSource))
            {
                builder["Data Source"] = dataSource;
                return builder.ConnectionString;
            }

            var baseDir = preferAppData ? DataDirectory : AppContext.BaseDirectory;
            builder["Data Source"] = Path.Combine(baseDir, dataSource);
            return builder.ConnectionString;
        }
    }
}
