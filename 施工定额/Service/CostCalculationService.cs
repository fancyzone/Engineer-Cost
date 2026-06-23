using 施工定额.Entity;
namespace 施工定额
{
    public class CostCalculationService
    {
        /// <summary>
        /// 对整个清单列表做一次全量重算
        /// </summary>
        public void RecalculateAll(List<Qingdan> qingdanList)
        {
            foreach (var qd in qingdanList)
                RecalculateQingdan(qd);
        }

        /// <summary>
        /// 重算单条清单（含它下属的所有定额和消耗量）
        /// </summary>
        public void RecalculateQingdan(Qingdan qd)
        {
            foreach (var dg in qd.定额列表)
                RecalculateDinge(dg);

            qd.综合合价 = qd.定额列表.Sum(d => d.定额合价);
            qd.综合单价 = qd.工程量 != 0
                ? Math.Round(qd.综合合价 / qd.工程量, 2)
                : 0;
        }

        /// <summary>
        /// 重算单条定额（含它下属的所有消耗量）
        /// </summary>
        public void RecalculateDinge(Dinge dg)
        {
            foreach (var xhl in dg.消耗量列表)
                RecalculateXiaohaoliang(xhl, dg.定额工程量);

            dg.定额合价 = dg.消耗量列表.Sum(x => x.市场价合计);
            dg.定额单价 = dg.定额工程量 != 0
                ? Math.Round(dg.定额合价 / dg.定额工程量, 2)
                : 0;
        }

        private void RecalculateXiaohaoliang(Xiaohaoliang xhl, decimal dingeWorkAmount)
        {
            xhl.数量 = xhl.含量 * dingeWorkAmount;
            xhl.市场价合计 = Math.Round(xhl.市场价 * xhl.数量, 2);
        }
    }
}