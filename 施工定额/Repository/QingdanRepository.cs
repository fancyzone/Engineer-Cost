using Dapper;
using System.Data.SQLite;
using 施工定额.Entity;

namespace 施工定额
{
    public class QingdanRepository
    {
        private readonly string _connStr;

        public QingdanRepository(string connStr)
        {
            _connStr = connStr;
        }

        /// <summary>
        /// 从数据库加载完整的清单树（含定额、消耗量）
        /// </summary>
        public List<Qingdan> LoadTree()
        {
            string sql = @"
            SELECT * FROM 清单;
            SELECT * FROM 定额_市政工程;
            SELECT * FROM 消耗量;";

            using var conn = new SQLiteConnection(_connStr);
            using var multi = conn.QueryMultiple(sql);

            var qingdanList = multi.Read<Qingdan>().ToList();
            var dingeList = multi.Read<Dinge>().ToList();
            var xhlList = multi.Read<Xiaohaoliang>().ToList();

            // 组装树结构
            var xhlLookup = xhlList.ToLookup(x => x.ID号);
            var dingeLookup = dingeList.ToLookup(d => d.清单编码 ?? "");

            foreach (var dg in dingeList)
                dg.消耗量列表 = xhlLookup[dg.ID号].ToList();

            foreach (var qd in qingdanList)
                qd.定额列表 = dingeLookup[qd.清单编码].ToList();

            return qingdanList;
        }

        /// <summary>
        /// 将一棵清单树持久化回数据库（事务保护）
        /// </summary>
        public void SaveTree(Qingdan qd)
        {
            using var conn = new SQLiteConnection(_connStr);
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                conn.Execute(@"UPDATE 清单 SET 
                清单名称=@清单名称, 项目特征=@项目特征, 单位=@单位,
                工程量=@工程量, 综合单价=@综合单价, 综合合价=@综合合价
                WHERE 清单编码=@清单编码", qd, tx);

                foreach (var dg in qd.定额列表)
                {
                    conn.Execute(@"UPDATE 定额_市政工程 SET
                    定额名称=@定额名称, 定额单位=@定额单位,
                    定额工程量=@定额工程量, 定额单价=@定额单价, 定额合价=@定额合价
                    WHERE 定额编码=@定额编码 AND 清单编码=@清单编码 AND ID号=@ID号",
                        dg, tx);

                    foreach (var xhl in dg.消耗量列表)
                    {
                        conn.Execute(@"UPDATE 消耗量 SET
                        含量=@含量, 数量=@数量, 定额基价=@定额基价,
                        市场价=@市场价, 市场价合计=@市场价合计
                        WHERE 定额编码=@定额编码 AND 清单编码=@清单编码
                        AND ID号=@ID号 AND 消耗量编码=@消耗量编码",
                            xhl, tx);
                    }
                }
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        /// <summary>
        /// 按消耗量类别汇总（用于人材机汇总界面的树形联动）
        /// </summary>
        public List<XiaohaoliangSummary> GetSummaryByCategory(string category)
        {
            string sql = """
                SELECT 消耗量类别,
                消耗量编码,
                消耗量名称,
                规格型号,
                消耗量单位,
                MAX(市场价)       AS 市场价, -- 同一种材料取最新/最高单价
                SUM(市场价合计)   AS 市场价合计   -- 所有定额用量的合计金额
                FROM 消耗量
                WHERE 消耗量类别 = @类别
                GROUP BY 消耗量编码, 消耗量名称, 规格型号, 消耗量单位, 消耗量类别
                """;
            using var conn = new SQLiteConnection(_connStr);
            return conn.Query<XiaohaoliangSummary>(sql, new { 类别 = category }).ToList();
        }
        /// <summary>
        /// 按消耗量编码批量更新市场价（跨所有定额）
        /// </summary>
        public void UpdateMarketPriceByCode(string 消耗量编码, decimal 新市场价)
        {
            using var conn = new SQLiteConnection(_connStr);
            conn.Execute(@"
                    UPDATE 消耗量 
                    SET 市场价 = @价格
                    WHERE 消耗量编码 = @编码",
                new { 价格 = 新市场价, 编码 = 消耗量编码 });
        }
        /// <summary>
        /// 删除一条清单及其下属的所有定额和消耗量
        /// </summary>
        public void DeleteQingdan(string qingdanCode)
        {
            using var conn = new SQLiteConnection(_connStr);
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // 先删消耗量，再删定额，最后删清单（依赖顺序）
                conn.Execute("DELETE FROM 消耗量 WHERE 清单编码 = @Code",
                    new { Code = qingdanCode }, tx);
                conn.Execute("DELETE FROM 定额_市政工程 WHERE 清单编码 = @Code",
                    new { Code = qingdanCode }, tx);
                conn.Execute("DELETE FROM 清单 WHERE 清单编码 = @Code",
                    new { Code = qingdanCode }, tx);

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