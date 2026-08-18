using Dapper;
using Microsoft.Data.Sqlite;
using 施工定额.Entity;

namespace 施工定额
{
    public class OtherProjectRepository
    {
        private readonly string _connStr;

        public static readonly string[] DefaultNames =
        {
            "暂列金额", "暂估价", "总承包服务费", "计日工"
        };

        public OtherProjectRepository(string connStr)
        {
            _connStr = connStr;
        }

        public List<OtherProjectItem> LoadOrSeed()
        {
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            EnsureTable(conn);
            SeedDefaults(conn);

            return conn.Query<OtherProjectItem>(
                @"SELECT 名称, 金额, 可编辑 FROM 其他项目
                  ORDER BY CASE 名称
                    WHEN '暂列金额' THEN 1
                    WHEN '暂估价' THEN 2
                    WHEN '总承包服务费' THEN 3
                    WHEN '计日工' THEN 4
                    ELSE 99 END").ToList();
        }

        public void SaveAmount(string name, decimal amount)
        {
            if (name == "暂估价")
                amount = 0;

            using var conn = new SqliteConnection(_connStr);
            conn.Execute(
                @"UPDATE 其他项目 SET 金额=@金额 WHERE 名称=@名称 AND 可编辑=1",
                new { 名称 = name, 金额 = amount });
        }

        private static void EnsureTable(SqliteConnection conn)
        {
            conn.Execute(@"
CREATE TABLE IF NOT EXISTS ""其他项目"" (
  ""名称"" TEXT NOT NULL PRIMARY KEY,
  ""金额"" REAL NOT NULL DEFAULT 0,
  ""可编辑"" INTEGER NOT NULL DEFAULT 1
);");
        }

        private static void SeedDefaults(SqliteConnection conn)
        {
            var seeds = new (string 名称, decimal 金额, int 可编辑)[]
            {
                ("暂列金额", 0, 1),
                ("暂估价", 0, 0),
                ("总承包服务费", 0, 1),
                ("计日工", 0, 1),
            };
            foreach (var s in seeds)
            {
                conn.Execute(
                    @"INSERT OR IGNORE INTO 其他项目 (名称, 金额, 可编辑)
                      VALUES (@名称, @金额, @可编辑)",
                    new { s.名称, s.金额, s.可编辑 });
            }
            conn.Execute(@"UPDATE 其他项目 SET 金额=0, 可编辑=0 WHERE 名称='暂估价'");
        }
    }
}
