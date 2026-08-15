using 施工定额.Entity;

namespace 施工定额.Helper
{
    /// <summary>
    /// 系统库参考数据缓存。
    /// 分类树与清单参考明细启动时加载；定额明细按分类懒加载。
    /// </summary>
    public interface IAppCache
    {
        IReadOnlyList<CategoryItem> QingdanCategories { get; }
        IReadOnlyList<CategoryItem> DingeCategories { get; }
        IReadOnlyList<QingdanDetail> QingdanDetails { get; }

        /// <summary>启动时加载分类树与清单参考数据（不含全量定额）。</summary>
        void LoadAll();

        /// <summary>
        /// 按分类 ID 集合获取定额明细（懒加载并缓存）。
        /// </summary>
        IReadOnlyList<Dinge> GetDingeByCategoryIds(IReadOnlyCollection<int> categoryIds);
    }
}
