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

        public void LoadAll()  // 不需要参数
        {
            _qingdanCategoryCache = DbHelper.LoadCategoryTreeToMemory("qingdan");
            _dingeCategoryCache = DbHelper.LoadCategoryTreeToMemory("dinge");
            using var conn = new SQLiteConnection(AppConfig.SystemDbConn);  // 统一走AppConfig
            _dingeDetailCache = conn.Query<Dinge>("SELECT * FROM 定额_市政工程").ToList();
        }
    }
}
