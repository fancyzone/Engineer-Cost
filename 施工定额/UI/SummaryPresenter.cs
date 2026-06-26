using System.ComponentModel;
using 施工定额.Entity;

namespace 施工定额.UI
{
    // 新建 施工定额/UI/SummaryPresenter.cs
    public class SummaryPresenter
    {
        private readonly BindingList<Qingdan> _qingdanList;
        private readonly QingdanRepository _repo;

        public SummaryPresenter(BindingList<Qingdan> qingdanList, QingdanRepository repo)
        {
            _qingdanList = qingdanList;
            _repo = repo;
        }

        public object GetCostSummaryData()
        {
            return new List<object>
        {
            new { Name = "分部分项费用", Price = _qingdanList.Sum(q => q.综合合价) },
            new { Name = "措施项目费用", Price = 0M },
            new { Name = "其他项目费用", Price = 0M },
            new { Name = "增值税",       Price = 0M },
            new { Name = "含税总价",     Price = _qingdanList.Sum(q => q.综合合价) * 1.09M },
        };
        }
        // SummaryPresenter.cs
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
