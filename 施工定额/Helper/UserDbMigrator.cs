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
                EnsureColumn(conn, "清单", "项目类别", "TEXT NOT NULL DEFAULT '分部分项'");
                NormalizeQingdanCategory(conn);
                EnsureOtherProjectTable(conn);
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

        private static void NormalizeQingdanCategory(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
UPDATE ""清单"" SET ""项目类别"" = CASE
  WHEN ""项目类别"" IS NULL OR TRIM(CAST(""项目类别"" AS TEXT)) = '' THEN '分部分项'
  WHEN CAST(""项目类别"" AS TEXT) IN ('0') THEN '分部分项'
  WHEN CAST(""项目类别"" AS TEXT) IN ('1','2') THEN '措施项目'
  WHEN CAST(""项目类别"" AS TEXT) IN ('3') THEN '其他项目'
  WHEN CAST(""项目类别"" AS TEXT) IN ('分部分项','措施项目','其他项目') THEN CAST(""项目类别"" AS TEXT)
  WHEN CAST(""项目类别"" AS TEXT) LIKE '%措施%' THEN '措施项目'
  WHEN CAST(""项目类别"" AS TEXT) LIKE '%其他%' THEN '其他项目'
  ELSE '分部分项'
END;";
            cmd.ExecuteNonQuery();
        }

        private static void EnsureOtherProjectTable(SqliteConnection conn)
        {
            using (var create = conn.CreateCommand())
            {
                create.CommandText = @"
CREATE TABLE IF NOT EXISTS ""其他项目"" (
  ""名称"" TEXT NOT NULL PRIMARY KEY,
  ""金额"" REAL NOT NULL DEFAULT 0,
  ""可编辑"" INTEGER NOT NULL DEFAULT 1
);";
                create.ExecuteNonQuery();
            }

            var seeds = new (string name, int editable)[]
            {
                ("暂列金额", 1),
                ("暂估价", 0),
                ("总承包服务费", 1),
                ("计日工", 1),
            };
            foreach (var (name, editable) in seeds)
            {
                using var ins = conn.CreateCommand();
                ins.CommandText =
                    @"INSERT OR IGNORE INTO ""其他项目"" (""名称"", ""金额"", ""可编辑"") VALUES ($n, 0, $e)";
                ins.Parameters.AddWithValue("$n", name);
                ins.Parameters.AddWithValue("$e", editable);
                ins.ExecuteNonQuery();
            }

            using var lockZg = conn.CreateCommand();
            lockZg.CommandText = @"UPDATE ""其他项目"" SET ""金额""=0, ""可编辑""=0 WHERE ""名称""='暂估价'";
            lockZg.ExecuteNonQuery();
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
