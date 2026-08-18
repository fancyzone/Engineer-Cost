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

        /// <summary>固定四项：编码 → 名称。</summary>
        public static readonly (string Code, string Name)[] Defaults =
        {
            ("QT-ZJJE", "暂列金额"),
            ("QT-ZGJ", "暂估价"),
            ("QT-ZCB", "总承包服务费"),
            ("QT-JRG", "计日工"),
        };

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
                    ELSE 99 END",
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

        private static void SeedDefaults(SqliteConnection conn)
        {
            foreach (var (code, name) in Defaults)
            {
                conn.Execute(
                    @"INSERT OR IGNORE INTO 清单
                        (清单编码, 清单名称, 项目特征, 单位, 工程量, 综合单价, 综合合价, 项目类别)
                      VALUES
                        (@Code, @Name, '', '', 0, 0, 0, @Cat)",
                    new { Code = code, Name = name, Cat = QingdanCategory.其他项目 });
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
