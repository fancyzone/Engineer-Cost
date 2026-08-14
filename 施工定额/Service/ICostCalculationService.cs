using 施工定额.Entity;

namespace 施工定额.Service
{
    /// <summary>
    /// 造价计算服务接口。
    /// 将计算逻辑与具体实现解耦，便于后续替换费率体系或编写单元测试。
    /// </summary>
    public interface ICostCalculationService
    {
        /// <summary>对整个清单列表做一次全量重算</summary>
        void RecalculateAll(List<Qingdan> qingdanList);

        /// <summary>重算单条清单（含下属所有定额和消耗量）</summary>
        void RecalculateQingdan(Qingdan qd);

        /// <summary>重算单条定额（含下属所有消耗量）</summary>
        void RecalculateDinge(Dinge dg);
    }
}
