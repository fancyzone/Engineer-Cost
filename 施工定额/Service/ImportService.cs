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

        /// <summary>
        /// 从系统库导入一条清单（连同它下属的所有定额和消耗量）到用户库。
        /// </summary>
        /// <param name="category">项目类别；空则默认「分部分项」。</param>
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

            var baseCode = sysCode.Trim();
            var existing = conn.Query<string>(
                @"SELECT 清单编码 FROM 清单
                  WHERE 清单编码 = @Base OR 清单编码 LIKE @Prefix",
                new { Base = baseCode, Prefix = baseCode + "%" }, tx).ToList();

            int maxSeq = 0;
            foreach (var code in existing)
            {
                if (string.IsNullOrEmpty(code)) continue;
                if (code.Length == baseCode.Length + 3
                    && code.StartsWith(baseCode, StringComparison.Ordinal)
                    && int.TryParse(code.AsSpan(baseCode.Length, 3), out var seq))
                {
                    if (seq > maxSeq) maxSeq = seq;
                }
            }

            for (int i = 1; i <= 999; i++)
            {
                int next = maxSeq + i;
                if (next > 999)
                    throw new InvalidOperationException(
                        $"清单编码 {baseCode} 的流水号已超过 999，无法继续插入。");
                string candidate = baseCode + next.ToString("D3");
                var hit = conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM 清单 WHERE 清单编码 = @C",
                    new { C = candidate }, tx);
                if (hit == 0)
                    return candidate;
            }

            throw new InvalidOperationException($"无法为 {baseCode} 分配唯一清单编码。");
        }

        public void ImportDinge(string targetQingdanCode, string sysId, string dingeCode, string name, string unit)
        {
            decimal qingdanWorkAmount;
            using (var conn = new SqliteConnection(_userConn))
            {
                qingdanWorkAmount = conn.ExecuteScalar<decimal>(
                    "SELECT 工程量 FROM 清单 WHERE 清单编码 = @Code",
                    new { Code = targetQingdanCode });
            }

            List<Xiaohaoliang> sysXhlList;

            using (var conn = new SqliteConnection(_sysConn))
            {
                sysXhlList = new List<Xiaohaoliang>();

                if (!string.IsNullOrEmpty(sysId))
                {
                    sysXhlList = conn.Query<Xiaohaoliang>(
                        "SELECT * FROM 消耗量 WHERE 定额ID = @Id",
                        new { Id = sysId }).ToList();
                }

                if (sysXhlList.Count == 0 && !string.IsNullOrEmpty(dingeCode))
                {
                    sysXhlList = conn.Query<Xiaohaoliang>(
                        "SELECT * FROM 消耗量 WHERE 定额编码 = @Code",
                        new { Code = dingeCode }).ToList();
                }
            }

            if (sysXhlList.Count == 0)
                throw new InvalidOperationException(
                    $"定额 [{dingeCode}]（系统ID={sysId}）在系统库中未找到消耗量明细。");

            string newId = Guid.NewGuid().ToString();

            foreach (var xhl in sysXhlList)
            {
                xhl.定额ID = newId;
                xhl.清单编码 = targetQingdanCode;
                xhl.定额编码 = dingeCode;
                xhl.市场价 = xhl.定额基价;
                xhl.数量 = xhl.含量 * qingdanWorkAmount;
                xhl.市场价合计 = Math.Round(xhl.市场价 * xhl.数量, 2);
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
                            (@ID号, @清单编码, @定额编码, @定额名称, @定额单位, @定额工程量, 0, 0, 1)",
                    new
                    {
                        ID号 = newId,
                        清单编码 = targetQingdanCode,
                        定额编码 = dingeCode,
                        定额名称 = name,
                        定额单位 = unit,
                        定额工程量 = qingdanWorkAmount
                    }, tx);

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
