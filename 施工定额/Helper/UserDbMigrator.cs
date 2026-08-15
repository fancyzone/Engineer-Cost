using Microsoft.Data.Sqlite;

namespace 施工定额.Helper
{
    /// <summary>
    /// 用户库轻量迁移：为已有库补齐新增列与常用索引，不破坏现有数据。
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
                EnsureIndex(conn, "idx_消耗量_定额ID_编码",
                    "CREATE INDEX IF NOT EXISTS \"idx_消耗量_定额ID_编码\" ON \"消耗量\"(\"定额ID\", \"消耗量编码\")");
                EnsureIndex(conn, "idx_定额_清单编码",
                    "CREATE INDEX IF NOT EXISTS \"idx_定额_清单编码\" ON \"定额_市政工程\"(\"清单编码\")");
                EnsureIndex(conn, "idx_消耗量_清单编码",
                    "CREATE INDEX IF NOT EXISTS \"idx_消耗量_清单编码\" ON \"消耗量\"(\"清单编码\")");
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

        private static void EnsureIndex(SqliteConnection conn, string indexName, string createSql)
        {
            using var check = conn.CreateCommand();
            check.CommandText =
                "SELECT 1 FROM sqlite_master WHERE type='index' AND name=@name LIMIT 1";
            check.Parameters.AddWithValue("@name", indexName);
            var exists = check.ExecuteScalar() != null;
            if (exists)
                return;

            using var cmd = conn.CreateCommand();
            cmd.CommandText = createSql;
            cmd.ExecuteNonQuery();
            AppLogger.Info($"用户库已创建索引 {indexName}");
        }
    }
}
