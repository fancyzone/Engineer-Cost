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

        public List<XiaohaoliangSummary> GetRenCaiJiSummary(string category)
        {
            return _repo.GetSummaryByCategory(category);
        }
    }
}
