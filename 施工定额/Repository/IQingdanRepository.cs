using 施工定额.Entity;

namespace 施工定额
{
    /// <summary>
    /// 清单树仓储抽象：加载、保存、删除与跨清单市场价更新。
    /// 便于单元测试与后续替换存储实现。
    /// </summary>
    public interface IQingdanRepository
    {
        /// <summary>从数据库加载完整的清单树（含定额、消耗量）</summary>
        List<Qingdan> LoadTree();

        /// <summary>将一棵清单树持久化回数据库（事务保护）</summary>
        void SaveTree(Qingdan qd);

        /// <summary>按消耗量编码批量更新市场价（跨所有定额）</summary>
        void UpdateMarketPriceByCode(string 消耗量编码, decimal 新市场价);

        /// <summary>删除一条清单及其下属的所有定额和消耗量</summary>
        void DeleteQingdan(string qingdanCode);
    }
}
