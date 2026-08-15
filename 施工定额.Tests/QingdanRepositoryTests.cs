using Dapper;
using Microsoft.Data.Sqlite;
using 施工定额;
using 施工定额.Entity;
using Xunit;

namespace 施工定额.Tests
{
    public class QingdanRepositoryTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly string _connStr;
        private readonly QingdanRepository _repo;

        public QingdanRepositoryTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"engcost_test_{Guid.NewGuid():N}.db");
            _connStr = $"Data Source={_dbPath}";
            CreateSchema();
            _repo = new QingdanRepository(_connStr);
        }

        public void Dispose()
        {
            try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
        }

        private void CreateSchema()
        {
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            conn.Execute(@"
CREATE TABLE 清单 (
  ID号 INTEGER PRIMARY KEY AUTOINCREMENT,
  清单编码 TEXT, 清单名称 TEXT, 项目特征 TEXT, 单位 TEXT,
  工程量 REAL, 综合单价 REAL, 综合合价 REAL
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
);");
        }

        private static Qingdan SampleQingdan()
        {
            var qd = new Qingdan
            {
                清单编码 = "Q-001",
                清单名称 = "测试清单",
                单位 = "m3",
                工程量 = 10m
            };
            var dg = new Dinge
            {
                ID号 = "dg-1",
                清单编码 = "Q-001",
                定额编码 = "D-001",
                定额名称 = "测试定额",
                定额单位 = "m3",
                定额工程量 = 10m,
                换算系数 = 1.5m
            };
            dg.消耗量列表.Add(new Xiaohaoliang
            {
                定额ID = "dg-1",
                清单编码 = "Q-001",
                定额编码 = "D-001",
                消耗量类别 = "人",
                消耗量编码 = "L1",
                消耗量名称 = "普工",
                含量 = 2m,
                数量 = 20m,
                市场价 = 100m,
                市场价合计 = 2000m
            });
            qd.定额列表.Add(dg);
            return qd;
        }

        [Fact]
        public void SaveDinge_ThenLoadTree_PersistsConversionFactor()
        {
            var qd = SampleQingdan();
            using (var conn = new SqliteConnection(_connStr))
            {
                conn.Open();
                conn.Execute(@"INSERT INTO 清单 (清单编码, 清单名称, 单位, 工程量, 综合单价, 综合合价)
VALUES (@清单编码, @清单名称, @单位, @工程量, 0, 0)", qd);
            }

            _repo.SaveDinge(qd.定额列表[0]);
            var loaded = _repo.LoadTree();
            Assert.Single(loaded);
            Assert.Single(loaded[0].定额列表);
            Assert.Equal(1.5m, loaded[0].定额列表[0].换算系数);
            Assert.Single(loaded[0].定额列表[0].消耗量列表);
            Assert.Equal(2m, loaded[0].定额列表[0].消耗量列表[0].含量);
        }

        [Fact]
        public void SaveDinge_Upsert_UpdatesExisting()
        {
            var qd = SampleQingdan();
            using (var conn = new SqliteConnection(_connStr))
            {
                conn.Open();
                conn.Execute(@"INSERT INTO 清单 (清单编码, 清单名称, 单位, 工程量, 综合单价, 综合合价)
VALUES (@清单编码, @清单名称, @单位, @工程量, 0, 0)", qd);
            }

            _repo.SaveDinge(qd.定额列表[0]);
            qd.定额列表[0].定额工程量 = 20m;
            qd.定额列表[0].换算系数 = 2m;
            _repo.SaveDinge(qd.定额列表[0]);

            var loaded = _repo.LoadTree();
            Assert.Equal(20m, loaded[0].定额列表[0].定额工程量);
            Assert.Equal(2m, loaded[0].定额列表[0].换算系数);
        }
    }
}
