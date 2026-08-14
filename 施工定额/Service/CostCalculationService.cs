using 施工定额.Entity;
using 施工定额.Helper;

namespace 施工定额
{
    /// <summary>
    /// 工程造价计算引擎。
    ///
    /// 计算链路：
    ///   消耗量.数量 = 含量 × 定额工程量
    ///   消耗量.市场价合计 = 市场价 × 数量
    ///   按消耗量类别汇总 → 人工费 / 材料费 / 机械费
    ///   直接费 = 人工费 + 材料费 + 机械费
    ///   管理费 = 基数(直接费或人工费) × 管理费率
    ///   利润   = (直接费 + 管理费) × 利润率
    ///   规费   = 人工费 × 规费率
    ///   定额合价 / 清单综合合价 = 直接费 + 管理费 + 利润（± 规费）
    ///   税金在项目级汇总时按增值税率计算
    /// </summary>
    public class CostCalculationService
    {
        private readonly FeeRateSettings _rates;

        public CostCalculationService() : this(AppConfig.FeeRates)
        {
        }

        public CostCalculationService(FeeRateSettings rates)
        {
            _rates = rates ?? new FeeRateSettings();
        }

        public FeeRateSettings Rates => _rates;

        /// <summary>
        /// 对整个清单列表做一次全量重算
        /// </summary>
        public void RecalculateAll(List<Qingdan> qingdanList)
        {
            foreach (var qd in qingdanList)
                RecalculateQingdan(qd);
        }

        /// <summary>
        /// 重算单条清单（含它下属的所有定额和消耗量，以及费用构成）
        /// </summary>
        public void RecalculateQingdan(Qingdan qd)
        {
            foreach (var dg in qd.定额列表)
                RecalculateDinge(dg);

            // 清单级费用构成 = 下属定额费用构成之和
            qd.费用构成.Reset();
            foreach (var dg in qd.定额列表)
            {
                qd.费用构成.人工费 += dg.费用构成.人工费;
                qd.费用构成.材料费 += dg.费用构成.材料费;
                qd.费用构成.机械费 += dg.费用构成.机械费;
                qd.费用构成.管理费 += dg.费用构成.管理费;
                qd.费用构成.利润 += dg.费用构成.利润;
                qd.费用构成.规费 += dg.费用构成.规费;
                qd.费用构成.不含税合价 += dg.费用构成.不含税合价;
            }

            // 税金在清单级按不含税合价计征（项目汇总时也会再算一遍，保持一致）
            qd.费用构成.税金 = Math.Round(qd.费用构成.不含税合价 * _rates.VatRate, 2);

            qd.综合合价 = qd.费用构成.不含税合价;
            qd.综合单价 = qd.工程量 != 0
                ? Math.Round(qd.综合合价 / qd.工程量, 2)
                : 0;
        }

        /// <summary>
        /// 重算单条定额（含它下属的所有消耗量，以及费用构成）
        /// </summary>
        public void RecalculateDinge(Dinge dg)
        {
            foreach (var xhl in dg.消耗量列表)
                RecalculateXiaohaoliang(xhl, dg.定额工程量);

            ApplyFeeBreakdown(dg.费用构成, dg.消耗量列表);

            dg.定额合价 = dg.费用构成.不含税合价;
            dg.定额单价 = dg.定额工程量 != 0
                ? Math.Round(dg.定额合价 / dg.定额工程量, 2)
                : 0;
        }

        /// <summary>
        /// 按消耗量列表计算完整费用构成（人材机 + 取费）
        /// </summary>
        public void ApplyFeeBreakdown(CostBreakdown breakdown, IEnumerable<Xiaohaoliang> xhlList)
        {
            breakdown.Reset();

            foreach (var x in xhlList)
            {
                switch (x.消耗量类别)
                {
                    case "人":
                        breakdown.人工费 += x.市场价合计;
                        break;
                    case "材":
                        breakdown.材料费 += x.市场价合计;
                        break;
                    case "机":
                        breakdown.机械费 += x.市场价合计;
                        break;
                    default:
                        // 未知类别并入材料费，避免金额丢失
                        breakdown.材料费 += x.市场价合计;
                        break;
                }
            }

            breakdown.人工费 = Math.Round(breakdown.人工费, 2);
            breakdown.材料费 = Math.Round(breakdown.材料费, 2);
            breakdown.机械费 = Math.Round(breakdown.机械费, 2);

            decimal overheadBase = string.Equals(_rates.OverheadBase, "Labor", StringComparison.OrdinalIgnoreCase)
                ? breakdown.人工费
                : breakdown.直接费;

            breakdown.管理费 = Math.Round(overheadBase * _rates.OverheadRate, 2);
            breakdown.利润 = Math.Round((breakdown.直接费 + breakdown.管理费) * _rates.ProfitRate, 2);
            breakdown.规费 = Math.Round(breakdown.人工费 * _rates.StatutoryFeeRate, 2);

            breakdown.不含税合价 = breakdown.直接费 + breakdown.管理费 + breakdown.利润;
            if (_rates.IncludeStatutoryInUnitPrice)
                breakdown.不含税合价 += breakdown.规费;

            breakdown.不含税合价 = Math.Round(breakdown.不含税合价, 2);
            breakdown.税金 = Math.Round(breakdown.不含税合价 * _rates.VatRate, 2);
        }

        /// <summary>
        /// 项目级汇总：分部分项合价、规费、税金、含税总价
        /// </summary>
        public ProjectCostSummary CalculateProjectSummary(IEnumerable<Qingdan> qingdanList)
        {
            var list = qingdanList.ToList();
            decimal billTotal = list.Sum(q => q.综合合价);
            decimal labor = list.Sum(q => q.费用构成.人工费);
            decimal material = list.Sum(q => q.费用构成.材料费);
            decimal machine = list.Sum(q => q.费用构成.机械费);
            decimal overhead = list.Sum(q => q.费用构成.管理费);
            decimal profit = list.Sum(q => q.费用构成.利润);

            // 规费：若单价已含规费则直接汇总，否则按项目人工费重算
            decimal statutory = _rates.IncludeStatutoryInUnitPrice
                ? list.Sum(q => q.费用构成.规费)
                : Math.Round(labor * _rates.StatutoryFeeRate, 2);

            decimal pretax = billTotal + (_rates.IncludeStatutoryInUnitPrice ? 0 : statutory);
            decimal tax = Math.Round(pretax * _rates.VatRate, 2);

            return new ProjectCostSummary
            {
                分部分项合价 = billTotal,
                人工费 = labor,
                材料费 = material,
                机械费 = machine,
                管理费 = overhead,
                利润 = profit,
                规费 = statutory,
                税金 = tax,
                不含税总价 = pretax,
                含税总价 = pretax + tax
            };
        }

        private void RecalculateXiaohaoliang(Xiaohaoliang xhl, decimal dingeWorkAmount)
        {
            xhl.数量 = xhl.含量 * dingeWorkAmount;
            xhl.市场价合计 = Math.Round(xhl.市场价 * xhl.数量, 2);
        }
    }

    /// <summary>
    /// 项目级造价汇总结果
    /// </summary>
    public class ProjectCostSummary
    {
        public decimal 分部分项合价 { get; set; }
        public decimal 人工费 { get; set; }
        public decimal 材料费 { get; set; }
        public decimal 机械费 { get; set; }
        public decimal 管理费 { get; set; }
        public decimal 利润 { get; set; }
        public decimal 规费 { get; set; }
        public decimal 税金 { get; set; }
        public decimal 不含税总价 { get; set; }
        public decimal 含税总价 { get; set; }
    }
}
