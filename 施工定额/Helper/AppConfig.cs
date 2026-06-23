using Microsoft.Extensions.Configuration;

namespace 施工定额.Helper
{
    public static class AppConfig
    {
        private static IConfiguration _config;

        static AppConfig()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)  // 始终以 exe 所在目录为基准
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();
        }

        public static string UserDbConn => ResolveConn("UserDb");
        public static string SystemDbConn => ResolveConn("SystemDb");

        private static string ResolveConn(string key)
        {
            var raw = _config.GetConnectionString(key)
         ?? throw new InvalidOperationException($"配置文件中找不到连接字符串: {key}");

            // 如果是相对路径，拼接成基于 exe 目录的绝对路径
            if (!Path.IsPathRooted(raw))
            {
                // 从连接字符串里提取文件名，拼上 exe 目录
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