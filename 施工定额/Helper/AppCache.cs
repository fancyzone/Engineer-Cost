using Dapper;
using Microsoft.Data.Sqlite;
using 施工定额.Entity;

namespace 施工定额.Helper
{
    /// <summary>
    /// 系统库参考数据缓存。
    /// 定额明细按分类懒加载，避免启动时全表 SELECT *。
    /// </summary>
    public class AppCache : IAppCache
    {
        private static readonly AppCache _instance = new();
        public static AppCache Instance => _instance;

        private List<CategoryItem> _qingdanCategoryCache = new();
        private List<CategoryItem> _dingeCategoryCache = new();
        private List<QingdanDetail> _qingdanDetailCache = new();

        private readonly Dictionary<int, List<Dinge>> _dingeByCategory = new();

        public IReadOnlyList<CategoryItem> QingdanCategories => _qingdanCategoryCache.AsReadOnly();
        public IReadOnlyList<CategoryItem> DingeCategories => _dingeCategoryCache.AsReadOnly();
        public IReadOnlyList<QingdanDetail> QingdanDetails => _qingdanDetailCache.AsReadOnly();

        private void ValidateDatabaseFiles()
        {
            var sysBuilder = new System.Data.Common.DbConnectionStringBuilder
            {
                ConnectionString = AppConfig.SystemDbConn
            };
            var userBuilder = new System.Data.Common.DbConnectionStringBuilder
            {
                ConnectionString = AppConfig.UserDbConn
            };

            string sysPath = sysBuilder["Data Source"]?.ToString() ?? "";
            string userPath = userBuilder["Data Source"]?.ToString() ?? "";

            if (!File.Exists(sysPath))
                throw new FileNotFoundException($"系统数据库文件不存在：\n{sysPath}");

            if (!File.Exists(userPath))
                throw new FileNotFoundException($"用户数据库文件不存在：\n{userPath}");
        }

        public void LoadAll()
        {
            ValidateDatabaseFiles();

            _qingdanCategoryCache = DbHelper.LoadCategoryTreeToMemory("qingdan");
            _dingeCategoryCache = DbHelper.LoadCategoryTreeToMemory("dinge");

            using var conn = new SqliteConnection(AppConfig.SystemDbConn);
            _qingdanDetailCache = conn.Query<QingdanDetail>(
                "SELECT 分类ID, 清单编码, 清单名称, 项目特征, 单位, 工程量计算规则, 工作内容 FROM 清单").ToList();

            _dingeByCategory.Clear();
        }

        public IReadOnlyList<Dinge> GetDingeByCategoryIds(IReadOnlyCollection<int> categoryIds)
        {
            if (categoryIds == null || categoryIds.Count == 0)
                return Array.Empty<Dinge>();

            var result = new List<Dinge>();
            var missing = new List<int>();

            foreach (var id in categoryIds.Distinct())
            {
                if (_dingeByCategory.TryGetValue(id, out var cached))
                    result.AddRange(cached);
                else
                    missing.Add(id);
            }

            if (missing.Count == 0)
                return result;

            using var conn = new SqliteConnection(AppConfig.SystemDbConn);
            var loaded = conn.Query<Dinge>(
                "SELECT * FROM 定额_市政工程 WHERE 分类ID IN @Ids",
                new { Ids = missing }).ToList();

            foreach (var group in loaded.GroupBy(d => d.分类ID))
            {
                var list = group.ToList();
                _dingeByCategory[group.Key] = list;
                result.AddRange(list);
            }

            foreach (var id in missing)
            {
                if (!_dingeByCategory.ContainsKey(id))
                    _dingeByCategory[id] = new List<Dinge>();
            }

            return result;
        }
    }
}
