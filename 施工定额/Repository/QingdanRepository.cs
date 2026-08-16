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

            foreach (var dg in dingeList)
            {
                if (dg.换算系数 == 0)
                    dg.换算系数 = 1m;
            }

            return qingdanList;
        }

        public void SaveTree(Qingdan qd) => SaveQingdan(qd);

        public void SaveQingdan(Qingdan qd)
        {
            if (qd == null) return;

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
                    UpsertDinge(conn, dg, tx);
                    foreach (var xhl in dg.消耗量列表)
                        UpsertXiaohaoliang(conn, xhl, tx);
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

        public void SaveDinge(Dinge dg)
        {
            if (dg == null) return;

            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                UpsertDinge(conn, dg, tx);
                foreach (var xhl in dg.消耗量列表)
                    UpsertXiaohaoliang(conn, xhl, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public void SaveXiaohaoliang(Xiaohaoliang xhl)
        {
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                UpsertXiaohaoliang(conn, xhl, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
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

        private static void UpsertDinge(SqliteConnection conn, Dinge dg, SqliteTransaction tx)
        {
            if (dg.换算系数 == 0)
                dg.换算系数 = 1m;

            conn.Execute(@"
INSERT INTO 定额_市政工程
    (ID号, 清单编码, 定额编码, 定额名称, 定额单位, 定额工程量, 定额单价, 定额合价, 换算系数)
VALUES
    (@ID号, @清单编码, @定额编码, @定额名称, @定额单位, @定额工程量, @定额单价, @定额合价, @换算系数)
ON CONFLICT(ID号) DO UPDATE SET
    清单编码=excluded.清单编码,
    定额编码=excluded.定额编码,
    定额名称=excluded.定额名称,
    定额单位=excluded.定额单位,
    定额工程量=excluded.定额工程量,
    定额单价=excluded.定额单价,
    定额合价=excluded.定额合价,
    换算系数=excluded.换算系数",
                dg, tx);
        }

        private static void UpsertXiaohaoliang(SqliteConnection conn, Xiaohaoliang xhl, SqliteTransaction tx)
        {
            conn.Execute(@"
INSERT INTO 消耗量
    (定额ID, 清单编码, 定额编码, 消耗量类别, 消耗量编码, 消耗量名称,
     规格型号, 消耗量单位, 含量, 数量, 定额基价, 市场价, 市场价合计)
VALUES
    (@定额ID, @清单编码, @定额编码, @消耗量类别, @消耗量编码, @消耗量名称,
     @规格型号, @消耗量单位, @含量, @数量, @定额基价, @市场价, @市场价合计)
ON CONFLICT(定额ID, 消耗量编码) DO UPDATE SET
    清单编码=excluded.清单编码,
    定额编码=excluded.定额编码,
    消耗量类别=excluded.消耗量类别,
    消耗量名称=excluded.消耗量名称,
    规格型号=excluded.规格型号,
    消耗量单位=excluded.消耗量单位,
    含量=excluded.含量,
    数量=excluded.数量,
    定额基价=excluded.定额基价,
    市场价=excluded.市场价,
    市场价合计=excluded.市场价合计",
                xhl, tx);
        }
    }
}
