using Dapper;
using System.Data.SQLite;
using 施工定额.Entity;

namespace 施工定额.Helper
{
    // 只管"从数据库预加载的静态参考数据"
    public class AppCache
    {
        private static readonly AppCache _instance = new();
        public static AppCache Instance => _instance;

        private List<CategoryItem> _qingdanCategoryCache = new();
        private List<CategoryItem> _dingeCategoryCache = new();
        private List<Dinge> _dingeDetailCache = new();

        public IReadOnlyList<CategoryItem> QingdanCategories => _qingdanCategoryCache.AsReadOnly();
        public IReadOnlyList<CategoryItem> DingeCategories => _dingeCategoryCache.AsReadOnly();
        public IReadOnlyList<Dinge> DingeDetails => _dingeDetailCache.AsReadOnly();
        private void ValidateDatabaseFiles()
        {
            // 从连接字符串里提取文件路径
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

        public void LoadAll()  // 不需要参数
        {
            // 在加载前先验证数据库文件是否存在
            ValidateDatabaseFiles();

            _qingdanCategoryCache = DbHelper.LoadCategoryTreeToMemory("qingdan");
            _dingeCategoryCache = DbHelper.LoadCategoryTreeToMemory("dinge");

            using var conn = new SQLiteConnection(AppConfig.SystemDbConn);
            _dingeDetailCache = conn.Query<Dinge>("SELECT * FROM 定额_市政工程").ToList();
        }
    }
}
