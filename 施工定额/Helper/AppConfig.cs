using Microsoft.Data.Sqlite;
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

        /// <summary>
        /// 确保 AppData 下用户库存在且具备必要表结构。
        /// 优先从程序目录复制模板；若仍无表则本地建表（空工程）。
        /// </summary>
        private static void EnsureUserDatabase()
        {
            var userPath = Path.Combine(DataDirectory, "userDB.db");
            var bundled = Path.Combine(AppContext.BaseDirectory, "userDB.db");

            try
            {
                if (!File.Exists(userPath) || !HasRequiredUserTables(userPath))
                {
                    if (File.Exists(bundled) && HasRequiredUserTables(bundled))
                    {
                        File.Copy(bundled, userPath, overwrite: true);
                        AppLogger.Info($"已从模板初始化用户库：{userPath}");
                    }
                }

                if (!HasRequiredUserTables(userPath))
                {
                    CreateUserSchema(userPath);
                    AppLogger.Info($"已创建空用户库表结构：{userPath}");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("初始化用户数据库失败", ex);
            }
        }

        private static bool HasRequiredUserTables(string dbPath)
        {
            if (!File.Exists(dbPath))
                return false;

            try
            {
                if (new FileInfo(dbPath).Length < 100)
                    return false;
            }
            catch
            {
                return false;
            }

            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name IN ('清单','定额_市政工程','消耗量')";
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                return count >= 3;
            }
            catch
            {
                return false;
            }
        }

        private static void CreateUserSchema(string dbPath)
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS ""清单"" (
	""ID号""	INTEGER UNIQUE,
	""标准号""	TEXT,
	""专业类别""	TEXT,
	""分部工程""	TEXT,
	""分项工程""	TEXT,
	""清单编码""	TEXT,
	""清单名称""	TEXT,
	""项目特征""	TEXT,
	""单位""	TEXT,
	""工程量""	REAL,
	""综合单价""	REAL,
	""综合合价""	REAL,
	""Level""	TEXT,
	PRIMARY KEY(""ID号"" AUTOINCREMENT)
);

CREATE TABLE IF NOT EXISTS ""定额_市政工程"" (
	""ID号""	TEXT NOT NULL UNIQUE,
	""清单编码""	TEXT,
	""册""	TEXT,
	""章""	TEXT,
	""节""	TEXT,
	""定额编码""	TEXT,
	""定额名称""	TEXT,
	""定额单位""	TEXT,
	""定额工程量""	REAL,
	""定额单价""	REAL,
	""定额合价""	REAL,
	""Level""	TEXT
);

CREATE TABLE IF NOT EXISTS ""消耗量"" (
	""定额ID""	TEXT,
	""清单编码""	TEXT,
	""定额编码""	TEXT,
	""消耗量类别""	TEXT,
	""消耗量编码""	TEXT,
	""消耗量名称""	TEXT,
	""消耗量单位""	TEXT,
	""含量""	REAL,
	""数量""	REAL,
	""定额基价""	REAL,
	""市场价""	REAL,
	""市场价合计""	REAL,
	""规格型号""	TEXT
);

CREATE TABLE IF NOT EXISTS ""人材机"" (
	""编码""	TEXT NOT NULL,
	""类别""	TEXT,
	""名称""	TEXT,
	""规格型号""	TEXT,
	""备注""	TEXT,
	PRIMARY KEY(""编码"")
);
";
            cmd.ExecuteNonQuery();
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
