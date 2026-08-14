using Dapper;
using Microsoft.Data.Sqlite;
using 施工定额.Entity;

namespace 施工定额
{
    public class QingdanRepository : IQingdanRepository
    {
        private readonly string _connStr;

        public QingdanRepository(string connStr)
        {
            _connStr = connStr;
        }

        public List<Qingdan> LoadTree()
        {
            const string sql = @"
            SELECT * FROM 清单;
            SELECT * FROM 定额_市政工程;
            SELECT * FROM 消耗量;";

            using var conn = new SqliteConnection(_connStr);
            using var multi = conn.QueryMultiple(sql);

            var qingdanList = multi.Read<Qingdan>().ToList();
            var dingeList = multi.Read<Dinge>().ToList();
            var xhlList = multi.Read<Xiaohaoliang>().ToList();

            var xhlLookup = xhlList.ToLookup(x => x.定额ID);
            var dingeLookup = dingeList.ToLookup(d => d.清单编码 ?? "");

            foreach (var dg in dingeList)
                dg.消耗量列表 = xhlLookup[dg.ID号].ToList();

            foreach (var qd in qingdanList)
                qd.定额列表 = dingeLookup[qd.清单编码].ToList();

            return qingdanList;
        }

        public void SaveTree(Qingdan qd)
        {
            using var conn = new SqliteConnection(_connStr);
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
                        含量=@含量, 数量=@数量, 定额基价=@定额基价, 市场价合计=@市场价合计
                        WHERE 定额ID=@定额ID AND 消耗量编码=@消耗量编码", xhl, tx);
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

        public void SaveQingdanHeader(Qingdan qd)
        {
            using var conn = new SqliteConnection(_connStr);
            conn.Execute(@"UPDATE 清单 SET 
                清单名称=@清单名称, 项目特征=@项目特征, 单位=@单位,
                工程量=@工程量, 综合单价=@综合单价, 综合合价=@综合合价
                WHERE 清单编码=@清单编码", qd);
        }

        public void SaveXiaohaoliang(Xiaohaoliang xhl)
        {
            using var conn = new SqliteConnection(_connStr);
            conn.Execute(@"UPDATE 消耗量 SET
                含量=@含量, 数量=@数量, 市场价合计=@市场价合计
                WHERE 定额ID=@定额ID AND 消耗量编码=@消耗量编码", xhl);
        }

        public void UpdateMarketPriceByCode(string 消耗量编码, decimal 新市场价)
        {
            using var conn = new SqliteConnection(_connStr);
            conn.Execute(@"
                UPDATE 消耗量 
                SET 市场价 = @价格
                WHERE 消耗量编码 = @编码",
                new { 价格 = 新市场价, 编码 = 消耗量编码 });
        }

        public void DeleteQingdan(string qingdanCode)
        {
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
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
