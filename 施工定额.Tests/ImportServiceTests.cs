using Dapper;
using Microsoft.Data.Sqlite;
using 施工定额.Service;
using Xunit;

namespace 施工定额.Tests
{
    public class ImportServiceTests : IDisposable
    {
        private readonly string _sysPath;
        private readonly string _userPath;
        private readonly string _sysConn;
        private readonly string _userConn;
        private readonly ImportService _svc;

        public ImportServiceTests()
        {
            _sysPath = Path.Combine(Path.GetTempPath(), $"engcost_sys_{Guid.NewGuid():N}.db");
            _userPath = Path.Combine(Path.GetTempPath(), $"engcost_user_{Guid.NewGuid():N}.db");
            _sysConn = $"Data Source={_sysPath}";
            _userConn = $"Data Source={_userPath}";
            CreateSystemSchema();
            CreateUserSchema();
            SeedSystemData();
            _svc = new ImportService(_sysConn, _userConn);
        }

        public void Dispose()
        {
            try { if (File.Exists(_sysPath)) File.Delete(_sysPath); } catch { }
            try { if (File.Exists(_userPath)) File.Delete(_userPath); } catch { }
        }

        private void CreateSystemSchema()
        {
            using var conn = new SqliteConnection(_sysConn);
            conn.Open();
            conn.Execute(@"
CREATE TABLE 清单 (
  清单编码 TEXT, 清单名称 TEXT, 项目特征 TEXT, 单位 TEXT
);
CREATE TABLE 定额_市政工程 (
  ID号 TEXT NOT NULL, 清单编码 TEXT, 定额编码 TEXT, 定额名称 TEXT, 定额单位 TEXT,
  定额工程量 REAL, 定额单价 REAL, 定额合价 REAL
);
CREATE TABLE 消耗量 (
  定额ID TEXT, 清单编码 TEXT, 定额编码 TEXT,
  消耗量类别 TEXT, 消耗量编码 TEXT, 消耗量名称 TEXT,
  规格型号 TEXT, 消耗量单位 TEXT,
  含量 REAL, 数量 REAL, 定额基价 REAL, 市场价 REAL, 市场价合计 REAL
);
");
        }

        private void CreateUserSchema()
        {
            using var conn = new SqliteConnection(_userConn);
            conn.Open();
            conn.Execute(@"
CREATE TABLE 清单 (
  ID号 INTEGER PRIMARY KEY AUTOINCREMENT,
  清单编码 TEXT, 清单名称 TEXT, 项目特征 TEXT, 单位 TEXT,
  工程量 REAL, 综合单价 REAL, 综合合价 REAL,
  项目类别 TEXT NOT NULL DEFAULT '分部分项'
);
CREATE TABLE 定额_市政工程 (
  ID号 TEXT NOT NULL UNIQUE,
  清单编码 TEXT, 定额编码 TEXT, 定额名称 TEXT, 定额单位 TEXT,
  定额工程量 REAL, 定额单价 REAL, 定额合价 REAL,
  换算系数 REAL NOT NULL DEFAULT 1
);
CREATE TABLE 消耗量 (
  定额ID TEXT, 清单编码 TEXT, 定额编码 TEXT,
  消耗量类别 TEXT, 消耗量编码 TEXT, 消耗量名称 TEXT,
  规格型号 TEXT, 消耗量单位 TEXT,
  含量 REAL, 数量 REAL, 定额基价 REAL, 市场价 REAL, 市场价合计 REAL
);
CREATE UNIQUE INDEX uidx_消耗量_定额ID_编码 ON 消耗量(定额ID, 消耗量编码);
");
        }

        private void SeedSystemData()
        {
            using var conn = new SqliteConnection(_sysConn);
            conn.Open();
            conn.Execute("INSERT INTO 清单 (清单编码, 清单名称, 项目特征, 单位) VALUES ('Q-SYS', '系统清单', '特征A', 'm3')");
            conn.Execute(@"INSERT INTO 定额_市政工程 (ID号, 清单编码, 定额编码, 定额名称, 定额单位, 定额工程量, 定额单价, 定额合价)
VALUES ('sys-dg-1', 'Q-SYS', 'D-100', '挖土', 'm3', 0, 0, 0)");
            conn.Execute(@"INSERT INTO 消耗量 (定额ID, 清单编码, 定额编码, 消耗量类别, 消耗量编码, 消耗量名称, 消耗量单位, 含量, 数量, 定额基价, 市场价, 市场价合计)
VALUES ('sys-dg-1', 'Q-SYS', 'D-100', '人', 'L1', '普工', '工日', 2, 0, 100, 100, 0)");
        }

        [Fact]
        public void ImportQingdan_CopiesDingeAndXiaohaoliang_WithNewIds()
        {
            _svc.ImportQingdan("Q-SYS", "系统清单", "特征A", "m3");

            using var conn = new SqliteConnection(_userConn);
            conn.Open();

            var qdCount = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM 清单 WHERE 清单编码='Q-SYS'");
            Assert.Equal(1, qdCount);

            var category = conn.ExecuteScalar<string>("SELECT 项目类别 FROM 清单 WHERE 清单编码='Q-SYS'");
            Assert.Equal("分部分项", category);

            var dgId = conn.ExecuteScalar<string>("SELECT ID号 FROM 定额_市政工程 WHERE 清单编码='Q-SYS'");
            Assert.False(string.IsNullOrEmpty(dgId));
            Assert.NotEqual("sys-dg-1", dgId);

            var xhlCount = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM 消耗量 WHERE 定额ID=@Id", new { Id = dgId });
            Assert.Equal(1, xhlCount);

            var factor = conn.ExecuteScalar<decimal>("SELECT 换算系数 FROM 定额_市政工程 WHERE ID号=@Id", new { Id = dgId });
            Assert.Equal(1m, factor);
        }

        [Fact]
        public void ImportDinge_AttachesToExistingQingdan()
        {
            using (var conn = new SqliteConnection(_userConn))
            {
                conn.Open();
                conn.Execute(@"INSERT INTO 清单 (清单编码, 清单名称, 单位, 工程量, 综合单价, 综合合价, 项目类别)
VALUES ('Q-USER', '用户清单', 'm3', 5, 0, 0, '分部分项')");
            }

            _svc.ImportDinge("Q-USER", "sys-dg-1", "D-100", "挖土", "m3");

            using var user = new SqliteConnection(_userConn);
            user.Open();
            var dgCount = user.ExecuteScalar<int>("SELECT COUNT(1) FROM 定额_市政工程 WHERE 清单编码='Q-USER'");
            Assert.Equal(1, dgCount);

            var work = user.ExecuteScalar<decimal>("SELECT 定额工程量 FROM 定额_市政工程 WHERE 清单编码='Q-USER'");
            Assert.Equal(5m, work);

            var qty = user.ExecuteScalar<decimal>("SELECT 数量 FROM 消耗量 WHERE 清单编码='Q-USER'");
            Assert.Equal(10m, qty);
        }

        [Fact]
        public void ImportDinge_NoXiaohaoliang_Throws()
        {
            using (var conn = new SqliteConnection(_userConn))
            {
                conn.Open();
                conn.Execute(@"INSERT INTO 清单 (清单编码, 清单名称, 单位, 工程量, 综合单价, 综合合价, 项目类别)
VALUES ('Q-USER', '用户清单', 'm3', 1, 0, 0, '分部分项')");
            }

            Assert.Throws<InvalidOperationException>(() =>
                _svc.ImportDinge("Q-USER", "missing-id", "NO-CODE", "无数据", "m3"));
        }
    }
}
