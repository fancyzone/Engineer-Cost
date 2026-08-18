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
                EnsureColumn(conn, "清单", "项目类别", "INTEGER NOT NULL DEFAULT 0");
                EnsureIndex(conn, "idx_定额_清单编码",
                    "CREATE INDEX IF NOT EXISTS \"idx_定额_清单编码\" ON \"定额_市政工程\"(\"清单编码\")");
                EnsureIndex(conn, "idx_消耗量_清单编码",
                    "CREATE INDEX IF NOT EXISTS \"idx_消耗量_清单编码\" ON \"消耗量\"(\"清单编码\")");
                EnsureIndex(conn, "idx_消耗量_编码",
                    "CREATE INDEX IF NOT EXISTS \"idx_消耗量_编码\" ON \"消耗量\"(\"消耗量编码\")");
                EnsureIndex(conn, "idx_清单_项目类别",
                    "CREATE INDEX IF NOT EXISTS \"idx_清单_项目类别\" ON \"清单\"(\"项目类别\")");

                EnsureUniqueXiaohaoliangIndex(conn);
            }
            catch (Exception ex)
            {
                AppLogger.Error("用户库迁移失败", ex);
            }
        }

        private static void EnsureUniqueXiaohaoliangIndex(SqliteConnection conn)
        {
            const string uniqueName = "uidx_消耗量_定额ID_编码";
            if (IndexExists(conn, uniqueName))
                return;

            using (var dedupe = conn.CreateCommand())
            {
                dedupe.CommandText = @"
DELETE FROM ""消耗量""
WHERE rowid NOT IN (
    SELECT MIN(rowid)
    FROM ""消耗量""
    WHERE ""定额ID"" IS NOT NULL AND ""消耗量编码"" IS NOT NULL
    GROUP BY ""定额ID"", ""消耗量编码""
)
AND ""定额ID"" IS NOT NULL AND ""消耗量编码"" IS NOT NULL;";
                var removed = dedupe.ExecuteNonQuery();
                if (removed > 0)
                    AppLogger.Info($"用户库消耗量去重删除 {removed} 行");
            }

            if (IndexExists(conn, "idx_消耗量_定额ID_编码"))
            {
                using var drop = conn.CreateCommand();
                drop.CommandText = "DROP INDEX IF EXISTS \"idx_消耗量_定额ID_编码\"";
                drop.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                $"CREATE UNIQUE INDEX IF NOT EXISTS \"{uniqueName}\" ON \"消耗量\"(\"定额ID\", \"消耗量编码\")";
            cmd.ExecuteNonQuery();
            AppLogger.Info($"用户库已创建唯一索引 {uniqueName}");
        }

        private static bool IndexExists(SqliteConnection conn, string indexName)
        {
            using var check = conn.CreateCommand();
            check.CommandText =
                "SELECT 1 FROM sqlite_master WHERE type='index' AND name=@name LIMIT 1";
            check.Parameters.AddWithValue("@name", indexName);
            return check.ExecuteScalar() != null;
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
            if (IndexExists(conn, indexName))
                return;

            using var cmd = conn.CreateCommand();
            cmd.CommandText = createSql;
            cmd.ExecuteNonQuery();
            AppLogger.Info($"用户库已创建索引 {indexName}");
        }
    }
}
