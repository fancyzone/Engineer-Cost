using 施工定额.Entity;
using 施工定额.Helper;
using 施工定额.Service;

namespace 施工定额
{
    public class CostCalculationService : ICostCalculationService
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

        public void RecalculateAll(List<Qingdan> qingdanList)
        {
            foreach (var qd in qingdanList)
            {
                // 其他项目金额由用户直接填写综合合价，不走定额重算
                if (QingdanCategory.IsOther(qd.项目类别))
                    continue;
                RecalculateQingdan(qd);
            }
        }

        public void RecalculateQingdan(Qingdan qd)
        {
            if (QingdanCategory.IsOther(qd.项目类别))
                return;

            foreach (var dg in qd.定额列表)
                RecalculateDinge(dg);

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

            qd.费用构成.税金 = Math.Round(qd.费用构成.不含税合价 * _rates.VatRate, 2);

            qd.综合合价 = qd.费用构成.不含税合价;
            qd.综合单价 = qd.工程量 != 0
                ? Math.Round(qd.综合合价 / qd.工程量, 2)
                : 0;
        }

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

        public ProjectCostSummary CalculateProjectSummary(IEnumerable<Qingdan> qingdanList)
        {
            var list = qingdanList.ToList();
            decimal fenbu = list.Where(q => QingdanCategory.IsFenBu(q.项目类别)).Sum(q => q.综合合价);
            decimal measure = list.Where(q => QingdanCategory.IsMeasure(q.项目类别)).Sum(q => q.综合合价);
            decimal other = list.Where(q => QingdanCategory.IsOther(q.项目类别)).Sum(q => q.综合合价);
            decimal billTotal = fenbu + measure + other;

            decimal labor = list.Sum(q => q.费用构成.人工费);
            decimal material = list.Sum(q => q.费用构成.材料费);
            decimal machine = list.Sum(q => q.费用构成.机械费);
            decimal overhead = list.Sum(q => q.费用构成.管理费);
            decimal profit = list.Sum(q => q.费用构成.利润);

            decimal statutory = _rates.IncludeStatutoryInUnitPrice
                ? list.Sum(q => q.费用构成.规费)
                : Math.Round(labor * _rates.StatutoryFeeRate, 2);

            decimal pretax = billTotal + (_rates.IncludeStatutoryInUnitPrice ? 0 : statutory);
            decimal tax = Math.Round(pretax * _rates.VatRate, 2);

            return new ProjectCostSummary
            {
                分部分项合价 = fenbu,
                措施项目合价 = measure,
                其他项目合价 = other,
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
}
