using Microsoft.Data.Sqlite;

namespace 施工定额.Helper
{
    /// <summary>
    /// 用户库轻量迁移：为已有库补齐新增列，不破坏现有数据。
    /// </summary>
    public static class UserDbMigrator
    {
        public static void Apply(string dbPath)
        {
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
                return;

            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();
                EnsureColumn(conn, "定额_市政工程", "换算系数", "REAL NOT NULL DEFAULT 1");
            }
            catch (Exception ex)
            {
                AppLogger.Error("用户库迁移失败", ex);
            }
        }

        private static void EnsureColumn(SqliteConnection conn, string table, string column, string typeSql)
        {
            using var check = conn.CreateCommand();
            check.CommandText = $"PRAGMA table_info(\"{table}\")";
            using var reader = check.ExecuteReader();
            while (reader.Read())
            {
                var name = reader["name"]?.ToString();
                if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            reader.Close();

            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {typeSql}";
            alter.ExecuteNonQuery();
            AppLogger.Info($"用户库已添加列 {table}.{column}");
        }
    }
}
