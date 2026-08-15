using 施工定额.Entity;

namespace 施工定额.Helper
{
    /// <summary>
    /// 系统库参考数据缓存。
    /// 分类树启动时加载；清单参考明细与定额明细均按分类懒加载。
    /// </summary>
    public interface IAppCache
    {
        IReadOnlyList<CategoryItem> QingdanCategories { get; }
        IReadOnlyList<CategoryItem> DingeCategories { get; }

        /// <summary>
        /// 已缓存的清单参考明细（可能不完整；请优先用 GetQingdanDetailsByCategoryIds）。
        /// </summary>
        IReadOnlyList<QingdanDetail> QingdanDetails { get; }

        /// <summary>启动时加载分类树（不含全量清单/定额明细）。</summary>
        void LoadAll();

        /// <summary>
        /// 按分类 ID 集合获取清单参考明细（懒加载并缓存）。
        /// </summary>
        IReadOnlyList<QingdanDetail> GetQingdanDetailsByCategoryIds(IReadOnlyCollection<int> categoryIds);

        /// <summary>
        /// 按分类 ID 集合获取定额明细（懒加载并缓存）。
        /// </summary>
        IReadOnlyList<Dinge> GetDingeByCategoryIds(IReadOnlyCollection<int> categoryIds);
    }
}
