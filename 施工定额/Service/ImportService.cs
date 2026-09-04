using Dapper;
using Microsoft.Data.Sqlite;
using 施工定额.Entity;

namespace 施工定额.Service
{
    public class ImportService : IImportService
    {
        private readonly string _sysConn;
        private readonly string _userConn;

        public ImportService(string sysConn, string userConn)
        {
            _sysConn = sysConn;
            _userConn = userConn;
        }

        public void ImportQingdan(string sysQingdanCode, string name, string feature, string unit,
            string? category = null, string? unitProjectCode = null)
        {
            var resolvedCategory = QingdanCategory.Normalize(category);
            var resolvedUnit = string.IsNullOrWhiteSpace(unitProjectCode)
                ? UnitProject.DefaultCode
                : unitProjectCode.Trim();
            List<Dinge> sysDingeList;
            List<Xiaohaoliang> sysXhlList;

            using (var conn = new SqliteConnection(_sysConn))
            {
                sysDingeList = conn.Query<Dinge>(
                    "SELECT * FROM 定额_市政工程 WHERE 清单编码 = @Code",
                    new { Code = sysQingdanCode }).ToList();

                sysXhlList = conn.Query<Xiaohaoliang>(
                    "SELECT * FROM 消耗量 WHERE 清单编码 = @Code",
                    new { Code = sysQingdanCode }).ToList();
            }

            var idMapping = new Dictionary<string, string>();
            foreach (var dg in sysDingeList)
            {
                string newId = Guid.NewGuid().ToString();
                if (!string.IsNullOrEmpty(dg.ID号))
                    idMapping[dg.ID号] = newId;

                dg.ID号 = newId;
                dg.定额工程量 = 0;
                dg.定额单价 = 0;
                dg.定额合价 = 0;
                dg.换算系数 = 1m;
            }

            foreach (var xhl in sysXhlList)
            {
                if (!string.IsNullOrEmpty(xhl.定额ID) && idMapping.TryGetValue(xhl.定额ID, out var newId))
                    xhl.定额ID = newId;

                xhl.数量 = 0;
                xhl.市场价合计 = 0;
            }

            using var userConn = new SqliteConnection(_userConn);
            userConn.Open();
            using var tx = userConn.BeginTransaction();
            try
            {
                string userCode = AllocateUniqueQingdanCode(userConn, tx, sysQingdanCode);

                foreach (var dg in sysDingeList)
                    dg.清单编码 = userCode;
                foreach (var xhl in sysXhlList)
                    xhl.清单编码 = userCode;

                userConn.Execute(@"
                    INSERT INTO 清单
                        (清单编码, 清单名称, 项目特征, 单位, 工程量, 综合单价, 综合合价, 项目类别, 单位工程编码)
                    VALUES
                        (@清单编码, @清单名称, @项目特征, @单位, 0, 0, 0, @项目类别, @单位工程编码)",
                    new
                    {
                        清单编码 = userCode,
                        清单名称 = name,
                        项目特征 = feature,
                        单位 = unit,
                        项目类别 = resolvedCategory,
                        单位工程编码 = resolvedUnit
                    }, tx);

                if (sysDingeList.Count > 0)
                    userConn.Execute(@"
                        INSERT INTO 定额_市政工程
                            (ID号, 清单编码, 定额编码, 定额名称, 定额单位, 定额工程量, 定额单价, 定额合价, 换算系数)
                        VALUES
                            (@ID号, @清单编码, @定额编码, @定额名称, @定额单位, @定额工程量, @定额单价, @定额合价, @换算系数)",
                        sysDingeList, tx);

                if (sysXhlList.Count > 0)
                    userConn.Execute(@"
                        INSERT OR IGNORE INTO 消耗量
                            (定额ID, 清单编码, 定额编码, 消耗量类别, 消耗量编码, 消耗量名称,
                             规格型号, 消耗量单位, 含量, 数量, 定额基价, 市场价, 市场价合计)
                        VALUES
                            (@定额ID, @清单编码, @定额编码, @消耗量类别, @消耗量编码, @消耗量名称,
                             @规格型号, @消耗量单位, @含量, @数量, @定额基价, @市场价, @市场价合计)",
                        sysXhlList, tx);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private static string AllocateUniqueQingdanCode(SqliteConnection conn, SqliteTransaction tx, string sysCode)
        {
            if (string.IsNullOrWhiteSpace(sysCode))
                throw new ArgumentException("系统清单编码不能为空", nameof(sysCode));

            string baseCode = sysCode.Trim();
            var existing = conn.Query<string>(
                "SELECT 清单编码 FROM 清单 WHERE 清单编码 = @Code OR 清单编码 LIKE @Like",
                new { Code = baseCode, Like = baseCode + "%" }, tx).ToList();

            if (!existing.Any(c => string.Equals(c, baseCode, StringComparison.Ordinal)))
            {
                // 若库中已有 base+数字后缀风格，仍走后缀；否则首次可用 base+001
            }

            int maxSuffix = 0;
            foreach (var c in existing)
            {
                if (c == null) continue;
                if (c.Length <= baseCode.Length) continue;
                if (!c.StartsWith(baseCode, StringComparison.Ordinal)) continue;
                var tail = c.Substring(baseCode.Length);
                if (tail.Length == 3 && int.TryParse(tail, out int n) && n > maxSuffix)
                    maxSuffix = n;
            }

            return baseCode + (maxSuffix + 1).ToString("D3");
        }

        public void ImportDinge(string targetQingdanCode, string sysId,
            string dingeCode, string name, string unit)
        {
            Dinge? sysDg;
            List<Xiaohaoliang> sysXhlList;

            using (var conn = new SqliteConnection(_sysConn))
            {
                sysDg = conn.QueryFirstOrDefault<Dinge>(
                    "SELECT * FROM 定额_市政工程 WHERE ID号 = @Id",
                    new { Id = sysId });
                if (sysDg == null)
                    throw new InvalidOperationException("系统库中未找到该定额。");

                sysXhlList = conn.Query<Xiaohaoliang>(
                    "SELECT * FROM 消耗量 WHERE 定额ID = @Id",
                    new { Id = sysId }).ToList();
            }

            string newId = Guid.NewGuid().ToString();
            sysDg.ID号 = newId;
            sysDg.清单编码 = targetQingdanCode;
            sysDg.定额编码 = dingeCode;
            sysDg.定额名称 = name;
            sysDg.定额单位 = unit;
            sysDg.定额工程量 = 0;
            sysDg.定额单价 = 0;
            sysDg.定额合价 = 0;
            sysDg.换算系数 = 1m;

            foreach (var xhl in sysXhlList)
            {
                xhl.定额ID = newId;
                xhl.清单编码 = targetQingdanCode;
                xhl.数量 = 0;
                xhl.市场价合计 = 0;
            }

            using var userConn = new SqliteConnection(_userConn);
            userConn.Open();
            using var tx = userConn.BeginTransaction();
            try
            {
                userConn.Execute(@"
                    INSERT INTO 定额_市政工程
                        (ID号, 清单编码, 定额编码, 定额名称, 定额单位, 定额工程量, 定额单价, 定额合价, 换算系数)
                    VALUES
                        (@ID号, @清单编码, @定额编码, @定额名称, @定额单位, @定额工程量, @定额单价, @定额合价, @换算系数)",
                    sysDg, tx);

                if (sysXhlList.Count > 0)
                    userConn.Execute(@"
                        INSERT OR IGNORE INTO 消耗量
                            (定额ID, 清单编码, 定额编码, 消耗量类别, 消耗量编码, 消耗量名称,
                             规格型号, 消耗量单位, 含量, 数量, 定额基价, 市场价, 市场价合计)
                        VALUES
                            (@定额ID, @清单编码, @定额编码, @消耗量类别, @消耗量编码, @消耗量名称,
                             @规格型号, @消耗量单位, @含量, @数量, @定额基价, @市场价, @市场价合计)",
                        sysXhlList, tx);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}
