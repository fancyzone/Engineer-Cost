using Dapper;
using System.Data.SQLite;
using 施工定额.Entity;
namespace 施工定额.Helper
{
    public static class DbHelper
    {

        /// <summary>
        /// 🔥 新增：统一从数据库中加载分类树到纯内存中
        /// </summary>
        /// <param name="type">"qingdan" 或 "dinge"</param>
        public static List<CategoryItem> LoadCategoryTreeToMemory(string type)
        {
            // 根据传入类型决定查询哪张分类表
            string tableName = type == "qingdan" ? "清单分类表" : "定额_市政工程分类表";
            string sql = $"SELECT 分类ID, 分类名称, 父ID FROM {tableName}";

            using (var conn = new SQLiteConnection(AppConfig.SystemDbConn))
            {
                // 1. 一次性把整张分类表拉取到内存中
                var allItems = conn.Query<CategoryItem>(sql).ToList();

                // 2. 使用父ID建立 O(1) 级别的快速查找索引
                var parentLookup = allItems.ToLookup(x => x.父ID);

                // 3. 遍历所有项，把子分类优雅地塞给对应的父亲
                foreach (var item in allItems)
                {
                    item.子分类列表 = parentLookup[item.分类ID].ToList();
                }

                // 4. 最终只返回顶级节点（父ID为0的节点），它们已经自带了完整的子孙树
                return allItems.Where(x => x.父ID == 0).ToList();
            }
        }
    }
}
