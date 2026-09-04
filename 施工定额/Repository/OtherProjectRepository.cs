using Dapper;
using Microsoft.Data.Sqlite;
using 施工定额.Entity;

namespace 施工定额
{
    /// <summary>
    /// 其他项目读写：数据落在「清单」表，项目类别=其他项目。
    /// </summary>
    public class OtherProjectRepository
    {
        private readonly string _connStr;

        public static readonly (string Code, string Name)[] Defaults =
        {
            ("QT-ZJJE", "暂列金额"),
            ("QT-ZGJ", "暂估价"),
            ("QT-ZCB", "总承包服务费"),
            ("QT-JRG", "计日工"),
        };

        private static readonly HashSet<string> DefaultCodes =
            new(Defaults.Select(d => d.Code), StringComparer.Ordinal);

        public OtherProjectRepository(string connStr)
        {
            _connStr = connStr;
        }

        public List<OtherProjectItem> LoadOrSeed()
        {
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            SeedDefaults(conn);
            MigrateFromLegacyTable(conn);

            var rows = conn.Query<(string 清单编码, string 清单名称, decimal 综合合价)>(
                @"SELECT 清单编码, 清单名称, IFNULL(综合合价,0) AS 综合合价
                  FROM 清单
                  WHERE 项目类别 = @Cat
                  ORDER BY CASE 清单编码
                    WHEN 'QT-ZJJE' THEN 1
                    WHEN 'QT-ZGJ' THEN 2
                    WHEN 'QT-ZCB' THEN 3
                    WHEN 'QT-JRG' THEN 4
                    ELSE 50 END,
                    清单编码",
                new { Cat = QingdanCategory.其他项目 }).ToList();

            return rows.Select(r => new OtherProjectItem
            {
                清单编码 = r.清单编码,
                名称 = r.清单名称,
                金额 = r.清单编码 == "QT-ZGJ" ? 0 : r.综合合价
            }).ToList();
        }

        public void SaveAmount(string codeOrName, decimal amount)
        {
            var isZgj = codeOrName is "暂估价" or "QT-ZGJ";
            if (isZgj)
                amount = 0;

            using var conn = new SqliteConnection(_connStr);
            conn.Execute(
                @"UPDATE 清单 SET 综合合价=@金额, 综合单价=0, 工程量=0
                  WHERE 项目类别=@Cat AND (清单编码=@Key OR 清单名称=@Key)",
                new { 金额 = amount, Cat = QingdanCategory.其他项目, Key = codeOrName });
        }

        public void SaveName(string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code) || code == "QT-ZGJ")
                return;
            name = (name ?? "").Trim();
            if (string.IsNullOrEmpty(name))
                return;

            using var conn = new SqliteConnection(_connStr);
            conn.Execute(
                @"UPDATE 清单 SET 清单名称=@Name
                  WHERE 清单编码=@Code AND 项目类别=@Cat",
                new { Name = name, Code = code, Cat = QingdanCategory.其他项目 });
        }

        public OtherProjectItem AddCustom(string name, decimal amount = 0)
        {
            name = string.IsNullOrWhiteSpace(name) ? "新增项目" : name.Trim();
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            string code = AllocateCustomCode(conn);
            conn.Execute(
                @"INSERT INTO 清单
                    (清单编码, 清单名称, 项目特征, 单位, 工程量, 综合单价, 综合合价, 项目类别, 单位工程编码)
                  VALUES
                    (@Code, @Name, '', '', 0, 0, @Amt, @Cat, @Unit)",
                new { Code = code, Name = name, Amt = amount, Cat = QingdanCategory.其他项目, Unit = UnitProject.DefaultCode });
            return new OtherProjectItem { 清单编码 = code, 名称 = name, 金额 = amount };
        }

        public bool Delete(string code)
        {
            if (string.IsNullOrEmpty(code) || code == "QT-ZGJ")
                return false;
            using var conn = new SqliteConnection(_connStr);
            var n = conn.Execute(
                @"DELETE FROM 清单 WHERE 清单编码=@Code AND 项目类别=@Cat",
                new { Code = code, Cat = QingdanCategory.其他项目 });
            return n > 0;
        }

        public static bool IsDefaultCode(string? code) =>
            !string.IsNullOrEmpty(code) && DefaultCodes.Contains(code);

        private static string AllocateCustomCode(SqliteConnection conn)
        {
            var codes = conn.Query<string>(
                @"SELECT 清单编码 FROM 清单
                  WHERE 项目类别=@Cat AND 清单编码 LIKE 'QT-C%'",
                new { Cat = QingdanCategory.其他项目 }).ToList();
            int max = 0;
            foreach (var c in codes)
            {
                if (c != null && c.StartsWith("QT-C", StringComparison.Ordinal)
                    && int.TryParse(c.AsSpan(4), out var n) && n > max)
                    max = n;
            }
            for (int i = 1; i < 10000; i++)
            {
                string candidate = "QT-C" + (max + i).ToString("D3");
                var hit = conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM 清单 WHERE 清单编码=@C", new { C = candidate });
                if (hit == 0) return candidate;
            }
            throw new InvalidOperationException("无法分配其他项目编码");
        }

        private static void SeedDefaults(SqliteConnection conn)
        {
            foreach (var (code, name) in Defaults)
            {
                conn.Execute(
                    @"INSERT INTO 清单
                        (清单编码, 清单名称, 项目特征, 单位, 工程量, 综合单价, 综合合价, 项目类别, 单位工程编码)
                      SELECT @Code, @Name, '', '', 0, 0, 0, @Cat, @Unit
                      WHERE NOT EXISTS (
                        SELECT 1 FROM 清单 WHERE 清单编码 = @Code AND 项目类别 = @Cat
                      )",
                    new { Code = code, Name = name, Cat = QingdanCategory.其他项目, Unit = UnitProject.DefaultCode });
            }

            conn.Execute(
                @"UPDATE 清单 SET 综合合价=0 WHERE 清单编码='QT-ZGJ' AND 项目类别=@Cat",
                new { Cat = QingdanCategory.其他项目 });
        }

        private static void MigrateFromLegacyTable(SqliteConnection conn)
        {
            var exists = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name='其他项目'");
            if (exists == 0) return;

            foreach (var (code, name) in Defaults)
            {
                var amount = conn.ExecuteScalar<decimal?>(
                    "SELECT 金额 FROM 其他项目 WHERE 名称=@Name", new { Name = name });
                if (amount == null) continue;
                if (code == "QT-ZGJ") amount = 0;
                conn.Execute(
                    @"UPDATE 清单 SET 综合合价=@Amt
                      WHERE 清单编码=@Code AND 项目类别=@Cat",
                    new { Amt = amount.Value, Code = code, Cat = QingdanCategory.其他项目 });
            }

            conn.Execute("DROP TABLE IF EXISTS \"其他项目\"");
        }
    }
}
