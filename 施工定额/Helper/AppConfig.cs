using Microsoft.Extensions.Configuration;
using 施工定额.Entity;

namespace 施工定额.Helper
{
    public static class AppConfig
    {
        private static IConfiguration _config;
        private static FeeRateSettings? _feeRates;

        static AppConfig()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();
        }

        public static string UserDbConn => ResolveConn("UserDb");
        public static string SystemDbConn => ResolveConn("SystemDb");
        public static string UpdateVersionInfoUrl => _config["UpdateSettings:VersionInfoUrl"] ?? "";
        public static string AppUpdateVersionInfoUrl => _config["UpdateSettings:AppVersionInfoUrl"] ?? "";

        /// <summary>
        /// 取费费率配置。从 appsettings.json 的 FeeSettings 节点读取，缺省时使用默认值。
        /// </summary>
        public static FeeRateSettings FeeRates
        {
            get
            {
                if (_feeRates != null)
                    return _feeRates;

                var settings = new FeeRateSettings();
                var section = _config.GetSection("FeeSettings");
                if (section.Exists())
                {
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
                }

                _feeRates = settings;
                return _feeRates;
            }
        }

        public static string SystemDbFilePath
        {
            get
            {
                var builder = new System.Data.Common.DbConnectionStringBuilder();
                builder.ConnectionString = SystemDbConn;
                return builder["Data Source"]?.ToString() ?? "";
            }
        }

        private static string ResolveConn(string key)
        {
            var raw = _config.GetConnectionString(key)
         ?? throw new InvalidOperationException($"配置文件中找不到连接字符串: {key}");

            if (!Path.IsPathRooted(raw))
            {
                var builder = new System.Data.Common.DbConnectionStringBuilder();
                builder.ConnectionString = raw;
                if (builder.TryGetValue("Data Source", out var fileName))
                {
                    builder["Data Source"] = Path.Combine(AppContext.BaseDirectory, fileName.ToString());
                }
                return builder.ConnectionString;
            }

            return Environment.ExpandEnvironmentVariables(raw);
        }
    }
}
