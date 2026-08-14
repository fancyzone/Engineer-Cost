using System.ComponentModel;
using 施工定额.Entity;
using 施工定额.Service;

namespace 施工定额.UI
{
    /// <summary>
    /// 清单相关业务编排：改价、改量、改含量、重载、保存、删除。
    /// Form 只负责把 UI 事件转发给本类，不直接碰计算与持久化。
    /// </summary>
    public class QingdanPresenter
    {
        private readonly IQingdanRepository _repo;
        private readonly ICostCalculationService _calcService;
        private readonly BindingList<Qingdan> _qingdanList;
        private readonly Action<DisplayType> _updateDisplay;

        public QingdanPresenter(
            IQingdanRepository repo,
            ICostCalculationService calcService,
            BindingList<Qingdan> qingdanList,
            Action<DisplayType> updateDisplay)
        {
            _repo = repo;
            _calcService = calcService;
            _qingdanList = qingdanList;
            _updateDisplay = updateDisplay;
        }

        public void OnMarketPriceChanged(Xiaohaoliang xhl, decimal newPrice)
        {
            var affectedQingdan = new List<Qingdan>();
            foreach (var qd in _qingdanList)
            {
                bool hit = false;
                foreach (var dg in qd.定额列表)
                {
                    foreach (var x in dg.消耗量列表)
                    {
                        if (x.消耗量编码 == xhl.消耗量编码)
                        {
                            x.市场价 = newPrice;
                            hit = true;
                        }
                    }
                }
                if (hit)
                    affectedQingdan.Add(qd);
            }

            _repo.UpdateMarketPriceByCode(xhl.消耗量编码, newPrice);

            foreach (var qd in affectedQingdan)
            {
                _calcService.RecalculateQingdan(qd);
                _repo.SaveTree(qd);
            }

            _updateDisplay(DisplayType.Qingdan);
            _updateDisplay(DisplayType.Dinge);
            _updateDisplay(DisplayType.Xiaohaoliang);
        }

        public void OnQingdanWorkAmountChanged(Qingdan qd)
        {
            foreach (var dg in qd.定额列表)
                dg.定额工程量 = qd.工程量;

            _calcService.RecalculateQingdan(qd);
            _repo.SaveTree(qd);

            _updateDisplay(DisplayType.Dinge);
            _updateDisplay(DisplayType.Xiaohaoliang);
            _updateDisplay(DisplayType.Qingdan);
        }

        public void OnXiaohaoliangHanliangChanged(Xiaohaoliang xhl, decimal newHanliang)
        {
            xhl.含量 = newHanliang;

            var ownerQd = _qingdanList
                .FirstOrDefault(q => q.定额列表
                    .Any(d => d.消耗量列表
                        .Any(x => x.定额ID == xhl.定额ID
                               && x.消耗量编码 == xhl.消耗量编码)));

            if (ownerQd == null) return;

            _calcService.RecalculateQingdan(ownerQd);
            _repo.SaveTree(ownerQd);

            _updateDisplay(DisplayType.Qingdan);
            _updateDisplay(DisplayType.Dinge);
            _updateDisplay(DisplayType.Xiaohaoliang);
        }

        public void OnDingeChanged(Qingdan qd)
        {
            if (qd == null) return;

            _calcService.RecalculateQingdan(qd);
            _repo.SaveTree(qd);

            _updateDisplay(DisplayType.Qingdan);
            _updateDisplay(DisplayType.Dinge);
            _updateDisplay(DisplayType.Xiaohaoliang);
        }

        public void SaveQingdanFields(Qingdan qd)
        {
            if (qd == null) return;

            _repo.SaveTree(qd);
            _updateDisplay(DisplayType.Qingdan);
        }

        public void DeleteQingdan(string qingdanCode)
        {
            if (string.IsNullOrEmpty(qingdanCode)) return;

            _repo.DeleteQingdan(qingdanCode);

            var toRemove = _qingdanList.FirstOrDefault(q => q.清单编码 == qingdanCode);
            if (toRemove != null)
                _qingdanList.Remove(toRemove);

            _updateDisplay(DisplayType.Qingdan);
            _updateDisplay(DisplayType.Dinge);
            _updateDisplay(DisplayType.Xiaohaoliang);
        }

        public void ReloadAll()
        {
            var freshList = _repo.LoadTree();
            foreach (var qd in freshList)
                _calcService.RecalculateQingdan(qd);

            _qingdanList.Clear();
            foreach (var qd in freshList)
                _qingdanList.Add(qd);
        }
    }
}
