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
                EnsureUnitProjectTable(conn);
                EnsureColumn(conn, "清单", "单位工程编码", "TEXT NOT NULL DEFAULT 'DW001'");
                BackfillUnitProjectCode(conn);
                NormalizeQingdanCategory(conn);
                SeedOtherItemsInQingdan(conn);
                EnsureIndex(conn, "idx_定额_清单编码",
                    "CREATE INDEX IF NOT EXISTS \"idx_定额_清单编码\" ON \"定额_市政工程\"(\"清单编码\")");
                EnsureIndex(conn, "idx_消耗量_清单编码",
                    "CREATE INDEX IF NOT EXISTS \"idx_消耗量_清单编码\" ON \"消耗量\"(\"清单编码\")");
                EnsureIndex(conn, "idx_消耗量_编码",
                    "CREATE INDEX IF NOT EXISTS \"idx_消耗量_编码\" ON \"消耗量\"(\"消耗量编码\")");
                EnsureIndex(conn, "idx_清单_项目类别",
                    "CREATE INDEX IF NOT EXISTS \"idx_清单_项目类别\" ON \"清单\"(\"项目类别\")");
                EnsureIndex(conn, "idx_清单_单位工程编码",
                    "CREATE INDEX IF NOT EXISTS \"idx_清单_单位工程编码\" ON \"清单\"(\"单位工程编码\")");

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

        private static void SeedOtherItemsInQingdan(SqliteConnection conn)
        {
            var seeds = new (string code, string name)[]
            {
                ("QT-ZJJE", "暂列金额"),
                ("QT-ZGJ", "暂估价"),
                ("QT-ZCB", "总承包服务费"),
                ("QT-JRG", "计日工"),
            };

            using (var dedupe = conn.CreateCommand())
            {
                dedupe.CommandText = @"
DELETE FROM ""清单""
WHERE ""项目类别"" = '其他项目'
  AND rowid NOT IN (
    SELECT MIN(rowid) FROM ""清单""
    WHERE ""项目类别"" = '其他项目'
    GROUP BY ""清单编码""
  );";
                var removed = dedupe.ExecuteNonQuery();
                if (removed > 0)
                    AppLogger.Info($"其他项目重复行已清理 {removed} 条");
            }

            foreach (var (code, name) in seeds)
            {
                using var ins = conn.CreateCommand();
                ins.CommandText = @"
INSERT INTO ""清单""
  (""清单编码"", ""清单名称"", ""项目特征"", ""单位"", ""工程量"", ""综合单价"", ""综合合价"", ""项目类别"", ""单位工程编码"")
SELECT $c, $n, '', '', 0, 0, 0, '其他项目', 'DW001'
WHERE NOT EXISTS (
  SELECT 1 FROM ""清单"" WHERE ""清单编码"" = $c AND ""项目类别"" = '其他项目'
);";
                ins.Parameters.AddWithValue("$c", code);
                ins.Parameters.AddWithValue("$n", name);
                ins.ExecuteNonQuery();
            }

            using var zg = conn.CreateCommand();
            zg.CommandText = @"UPDATE ""清单"" SET ""综合合价""=0 WHERE ""清单编码""='QT-ZGJ' AND ""项目类别""='其他项目'";
            zg.ExecuteNonQuery();

            using (var check = conn.CreateCommand())
            {
                check.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name='其他项目'";
                var exists = Convert.ToInt32(check.ExecuteScalar());
                if (exists > 0)
                {
                    foreach (var (code, name) in seeds)
                    {
                        using var sel = conn.CreateCommand();
                        sel.CommandText = @"SELECT ""金额"" FROM ""其他项目"" WHERE ""名称""=$n";
                        sel.Parameters.AddWithValue("$n", name);
                        var val = sel.ExecuteScalar();
                        if (val == null || val is DBNull) continue;
                        var amt = code == "QT-ZGJ" ? 0m : Convert.ToDecimal(val);
                        using var upd = conn.CreateCommand();
                        upd.CommandText = @"UPDATE ""清单"" SET ""综合合价""=$a WHERE ""清单编码""=$c AND ""项目类别""='其他项目'";
                        upd.Parameters.AddWithValue("$a", amt);
                        upd.Parameters.AddWithValue("$c", code);
                        upd.ExecuteNonQuery();
                    }
                    using var drop = conn.CreateCommand();
                    drop.CommandText = "DROP TABLE IF EXISTS \"其他项目\"";
                    drop.ExecuteNonQuery();
                    AppLogger.Info("已将旧「其他项目」表数据迁入清单并删除旧表");
                }
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

        private static void EnsureUnitProjectTable(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS ""单位工程"" (
	""编码""	TEXT NOT NULL PRIMARY KEY,
	""名称""	TEXT NOT NULL,
	""排序""	INTEGER NOT NULL DEFAULT 0
);
INSERT INTO ""单位工程"" (""编码"", ""名称"", ""排序"")
SELECT 'DW001', '默认单位工程', 0
WHERE NOT EXISTS (SELECT 1 FROM ""单位工程"" WHERE ""编码"" = 'DW001');
";
            cmd.ExecuteNonQuery();
        }

        private static void BackfillUnitProjectCode(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
UPDATE ""清单""
SET ""单位工程编码"" = 'DW001'
WHERE ""单位工程编码"" IS NULL OR TRIM(CAST(""单位工程编码"" AS TEXT)) = '';
";
            cmd.ExecuteNonQuery();
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
