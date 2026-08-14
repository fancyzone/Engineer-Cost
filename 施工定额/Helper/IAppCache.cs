using 施工定额.Entity;

namespace 施工定额.Helper
{
    /// <summary>
    /// 系统库静态参考数据缓存抽象（分类树、清单/定额参考明细）。
    /// </summary>
    public interface IAppCache
    {
        IReadOnlyList<CategoryItem> QingdanCategories { get; }
        IReadOnlyList<CategoryItem> DingeCategories { get; }
        IReadOnlyList<QingdanDetail> QingdanDetails { get; }
        IReadOnlyList<Dinge> DingeDetails { get; }

        void LoadAll();
    }
}
