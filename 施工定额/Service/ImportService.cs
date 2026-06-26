using Dapper;
using System.Data.SQLite;
using 施工定额.Entity;

namespace 施工定额.Service
{
    public class ImportService
    {
        private readonly string _sysConn;
        private readonly string _userConn;

        public ImportService(string sysConn, string userConn)
        {
            _sysConn = sysConn;
            _userConn = userConn;
        }

        /// <summary>
        /// 从系统库导入一条清单（连同它下属的所有定额和消耗量）到用户库
        /// </summary>
        public void ImportQingdan(string sysQingdanCode, string name, string feature, string unit)
        {
            // 1. 从系统库捞取该清单下的所有定额和消耗量
            List<Dinge> sysDingeList;
            List<Xiaohaoliang> sysXhlList;

            using (var conn = new SQLiteConnection(_sysConn))
            {
                sysDingeList = conn.Query<Dinge>(
                    "SELECT * FROM 定额_市政工程 WHERE 清单编码 = @Code",
                    new { Code = sysQingdanCode }).ToList();

                sysXhlList = conn.Query<Xiaohaoliang>(
                    "SELECT * FROM 消耗量 WHERE 清单编码 = @Code",
                    new { Code = sysQingdanCode }).ToList();
            }

            // 2. 为每条定额生成新的隔离 GUID，并建立旧ID → 新ID的映射
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
            }

            // 3. 消耗量的 ID号 换成对应的新 GUID
            foreach (var xhl in sysXhlList)
            {
                if (!string.IsNullOrEmpty(xhl.定额ID) && idMapping.TryGetValue(xhl.定额ID, out var newId))
                    xhl.定额ID = newId;

                xhl.数量 = 0;
                xhl.市场价合计 = 0;
            }

            // 4. 写入用户库
            using var userConn = new SQLiteConnection(_userConn);
            userConn.Open();
            using var tx = userConn.BeginTransaction();
            try
            {
                userConn.Execute(@"
                    INSERT OR IGNORE INTO 清单
                        (清单编码, 清单名称, 项目特征, 单位, 工程量, 综合单价, 综合合价)
                    VALUES
                        (@清单编码, @清单名称, @项目特征, @单位, 0, 0, 0)",
                    new { 清单编码 = sysQingdanCode, 清单名称 = name, 项目特征 = feature, 单位 = unit },
                    tx);

                if (sysDingeList.Count > 0)
                    userConn.Execute(@"
                        INSERT INTO 定额_市政工程
                            (ID号, 清单编码, 定额编码, 定额名称, 定额单位, 定额工程量, 定额单价, 定额合价)
                        VALUES
                            (@ID号, @清单编码, @定额编码, @定额名称, @定额单位, @定额工程量, @定额单价, @定额合价)",
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

        /// <summary>
        /// 从系统库导入单条定额（连同它的消耗量）到用户库的指定清单下
        /// </summary>
        public void ImportDinge(string targetQingdanCode, string sysId,
                          string dingeCode, string name, string unit)
        {
            // 1. 读取目标清单的工程量
            decimal qingdanWorkAmount = 0;
            using (var conn = new SQLiteConnection(_userConn))
            {
                qingdanWorkAmount = conn.ExecuteScalar<decimal>(
                    "SELECT 工程量 FROM 清单 WHERE 清单编码 = @Code",
                    new { Code = targetQingdanCode });
            }

            // 2. 从系统库捞取该定额的所有消耗量
            List<Xiaohaoliang> sysXhlList;
            using (var conn = new SQLiteConnection(_sysConn))
            {
                sysXhlList = conn.Query<Xiaohaoliang>(
                    "SELECT * FROM 消耗量 WHERE 定额编码 = @Code",
                    new { Code = dingeCode }).ToList();
            }

            if (sysXhlList.Count == 0)
                throw new InvalidOperationException($"定额 [{dingeCode}] 在系统库中未找到消耗量明细。");

            // 3. 生成新的隔离 GUID
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

            // 4. 写入用户库
            using var userConn = new SQLiteConnection(_userConn);
            userConn.Open();
            using var tx = userConn.BeginTransaction();
            try
            {
                userConn.Execute(@"
                        INSERT INTO 定额_市政工程
                            (ID号, 清单编码, 定额编码, 定额名称, 定额单位, 定额工程量, 定额单价, 定额合价)
                        VALUES
                            (@ID号, @清单编码, @定额编码, @定额名称, @定额单位, @定额工程量, 0, 0)",
                    new
                    {
                        ID号 = newId,
                        清单编码 = targetQingdanCode,
                        定额编码 = dingeCode,
                        定额名称 = name,
                        定额单位 = unit,
                        定额工程量 = qingdanWorkAmount
                    },
                    tx);

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