using Dapper;
using Microsoft.Data.Sqlite;
using 施工定额.Entity;

namespace 施工定额.Helper
{
    /// <summary>
    /// 系统库静态参考数据缓存。
    /// 实现 IAppCache；默认仍提供 Instance 便于启动阶段使用。
    /// </summary>
    public class AppCache : IAppCache
    {
        private static readonly AppCache _instance = new();
        public static AppCache Instance => _instance;

        private List<CategoryItem> _qingdanCategoryCache = new();
        private List<CategoryItem> _dingeCategoryCache = new();
        private List<QingdanDetail> _qingdanDetailCache = new();
        private List<Dinge> _dingeDetailCache = new();

        public IReadOnlyList<CategoryItem> QingdanCategories => _qingdanCategoryCache.AsReadOnly();
        public IReadOnlyList<CategoryItem> DingeCategories => _dingeCategoryCache.AsReadOnly();
        public IReadOnlyList<QingdanDetail> QingdanDetails => _qingdanDetailCache.AsReadOnly();
        public IReadOnlyList<Dinge> DingeDetails => _dingeDetailCache.AsReadOnly();

        private void ValidateDatabaseFiles()
        {
            var sysBuilder = new System.Data.Common.DbConnectionStringBuilder();
            sysBuilder.ConnectionString = AppConfig.SystemDbConn;

            var userBuilder = new System.Data.Common.DbConnectionStringBuilder();
            userBuilder.ConnectionString = AppConfig.UserDbConn;

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
            _dingeDetailCache = conn.Query<Dinge>("SELECT * FROM 定额_市政工程").ToList();

            _qingdanDetailCache = conn.Query<QingdanDetail>(
                "SELECT 分类ID, 清单编码, 清单名称, 项目特征, 单位, 工程量计算规则, 工作内容 FROM 清单").ToList();
        }
    }
}
