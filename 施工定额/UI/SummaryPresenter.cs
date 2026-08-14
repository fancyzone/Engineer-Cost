using System.ComponentModel;
using 施工定额.Entity;

namespace 施工定额.UI
{
    public class SummaryPresenter
    {
        private readonly BindingList<Qingdan> _qingdanList;
        private readonly QingdanRepository _repo;
        private readonly CostCalculationService _calcService;

        public SummaryPresenter(BindingList<Qingdan> qingdanList, QingdanRepository repo)
            : this(qingdanList, repo, new CostCalculationService())
        {
        }

        public SummaryPresenter(
            BindingList<Qingdan> qingdanList,
            QingdanRepository repo,
            CostCalculationService calcService)
        {
            _qingdanList = qingdanList;
            _repo = repo;
            _calcService = calcService;
        }

        public object GetCostSummaryData()
        {
            var summary = _calcService.CalculateProjectSummary(_qingdanList);

            return new List<object>
            {
                new { Name = "分部分项费用", Price = summary.分部分项合价 },
                new { Name = "其中：人工费", Price = summary.人工费 },
                new { Name = "其中：材料费", Price = summary.材料费 },
                new { Name = "其中：机械费", Price = summary.机械费 },
                new { Name = "其中：管理费", Price = summary.管理费 },
                new { Name = "其中：利润",   Price = summary.利润 },
                new { Name = "措施项目费用", Price = 0M },
                new { Name = "其他项目费用", Price = 0M },
                new { Name = "规费",         Price = summary.规费 },
                new { Name = "增值税",       Price = summary.税金 },
                new { Name = "不含税总价",   Price = summary.不含税总价 },
                new { Name = "含税总价",     Price = summary.含税总价 },
            };
        }

        public List<XiaohaoliangSummary> GetRenCaiJiSummaryFromMemory(string category)
        {
            return _qingdanList
                .SelectMany(q => q.定额列表)
                .SelectMany(d => d.消耗量列表)
                .Where(x => string.IsNullOrEmpty(category) || x.消耗量类别 == category)
                .GroupBy(x => new { x.消耗量类别, x.消耗量编码, x.消耗量名称, x.规格型号, x.消耗量单位 })
                .Select(g => new XiaohaoliangSummary
                {
                    消耗量类别 = g.Key.消耗量类别,
                    消耗量编码 = g.Key.消耗量编码,
                    消耗量名称 = g.Key.消耗量名称,
                    规格型号 = g.Key.规格型号,
                    消耗量单位 = g.Key.消耗量单位,
                    市场价 = g.Max(x => x.市场价),
                    市场价合计 = g.Sum(x => x.市场价合计)
                })
                .ToList();
        }
    }
}
