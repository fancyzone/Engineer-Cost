using System.ComponentModel;
using 施工定额.Entity;
using 施工定额.Helper;
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
        private ICostCalculationService _calcService;
        private readonly BindingList<Qingdan> _qingdanList;
        private readonly Action<DisplayType> _updateDisplay;

        private Dictionary<string, List<Xiaohaoliang>> _xhlByCode = new(StringComparer.Ordinal);

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

        public void ReplaceCalcService(ICostCalculationService calcService)
        {
            _calcService = calcService ?? throw new ArgumentNullException(nameof(calcService));
            foreach (var qd in _qingdanList)
            {
                _calcService.RecalculateQingdan(qd);
                PersistQingdanFine(qd);
            }
            RefreshAll();
        }

        public void OnMarketPriceChanged(Xiaohaoliang xhl, decimal newPrice)
        {
            var code = xhl.消耗量编码 ?? "";
            var affectedQingdan = new HashSet<Qingdan>();

            if (!_xhlByCode.TryGetValue(code, out var list))
            {
                RebuildIndex();
                _xhlByCode.TryGetValue(code, out list);
            }

            if (list != null)
            {
                foreach (var x in list)
                {
                    x.市场价 = newPrice;
                    var owner = FindOwnerQingdan(x);
                    if (owner != null)
                        affectedQingdan.Add(owner);
                }
            }

            _repo.UpdateMarketPriceByCode(code, newPrice);

            foreach (var qd in affectedQingdan)
            {
                _calcService.RecalculateQingdan(qd);
                PersistQingdanFine(qd);
            }

            RefreshAll();
        }

        public void OnQingdanWorkAmountChanged(Qingdan qd)
        {
            foreach (var dg in qd.定额列表)
            {
                var factor = dg.换算系数 == 0 ? 1m : dg.换算系数;
                dg.定额工程量 = qd.工程量 * factor;
            }

            _calcService.RecalculateQingdan(qd);
            PersistQingdanFine(qd);
            RefreshAll();
        }

        public void OnXiaohaoliangHanliangChanged(Xiaohaoliang xhl, decimal newHanliang)
        {
            xhl.含量 = newHanliang;
            var ownerQd = FindOwnerQingdan(xhl);
            if (ownerQd == null) return;

            _calcService.RecalculateQingdan(ownerQd);
            PersistQingdanFine(ownerQd);
            RefreshAll();
        }

        public void OnDingeChanged(Qingdan qd)
        {
            if (qd == null) return;
            _calcService.RecalculateQingdan(qd);
            PersistQingdanFine(qd);
            RefreshAll();
        }

        public void OnDingeConversionFactorChanged(Qingdan qd, Dinge dg)
        {
            if (qd == null || dg == null) return;
            var factor = dg.换算系数 == 0 ? 1m : dg.换算系数;
            dg.定额工程量 = qd.工程量 * factor;
            OnDingeChanged(qd);
        }

        public void SaveQingdanFields(Qingdan qd)
        {
            if (qd == null) return;
            _repo.SaveQingdanHeader(qd);
            _updateDisplay(DisplayType.Qingdan);
        }

        public void DeleteQingdan(string qingdanCode)
        {
            if (string.IsNullOrEmpty(qingdanCode)) return;
            _repo.DeleteQingdan(qingdanCode);

            var toRemove = _qingdanList.FirstOrDefault(q => q.清单编码 == qingdanCode);
            if (toRemove != null)
                _qingdanList.Remove(toRemove);

            RebuildIndex();
            RefreshAll();
        }

        public void ReloadAll()
        {
            var freshList = _repo.LoadTree();
            foreach (var qd in freshList)
                _calcService.RecalculateQingdan(qd);

            _qingdanList.ReplaceAll(freshList);
            RebuildIndex();
        }

        private void PersistQingdanFine(Qingdan qd)
        {
            _repo.SaveQingdanHeader(qd);
            foreach (var dg in qd.定额列表)
                _repo.SaveDinge(dg);
        }

        private void RefreshAll()
        {
            _updateDisplay(DisplayType.Qingdan);
            _updateDisplay(DisplayType.Dinge);
            _updateDisplay(DisplayType.Xiaohaoliang);
        }

        private void RebuildIndex()
        {
            _xhlByCode = new Dictionary<string, List<Xiaohaoliang>>(StringComparer.Ordinal);
            foreach (var qd in _qingdanList)
            {
                foreach (var dg in qd.定额列表)
                {
                    foreach (var x in dg.消耗量列表)
                    {
                        var code = x.消耗量编码 ?? "";
                        if (!_xhlByCode.TryGetValue(code, out var list))
                        {
                            list = new List<Xiaohaoliang>();
                            _xhlByCode[code] = list;
                        }
                        list.Add(x);
                    }
                }
            }
        }

        private Qingdan? FindOwnerQingdan(Xiaohaoliang xhl)
        {
            return _qingdanList.FirstOrDefault(q =>
                q.定额列表.Any(d =>
                    d.消耗量列表.Any(x =>
                        ReferenceEquals(x, xhl)
                        || (x.定额ID == xhl.定额ID && x.消耗量编码 == xhl.消耗量编码))));
        }
    }
}
