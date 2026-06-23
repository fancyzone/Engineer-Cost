using System.ComponentModel;
using 施工定额.Entity;

namespace 施工定额.UI
{
    // 新建 施工定额/UI/QingdanPresenter.cs
    public class QingdanPresenter
    {
        private readonly QingdanRepository _repo;
        private readonly CostCalculationService _calcService;
        private readonly BindingList<Qingdan> _qingdanList;
        private readonly Action<Form1.DisplayType> _updateDisplay;

        public QingdanPresenter(
            QingdanRepository repo,
            CostCalculationService calcService,
            BindingList<Qingdan> qingdanList,
            Action<Form1.DisplayType> updateDisplay)
        {
            _repo = repo;
            _calcService = calcService;
            _qingdanList = qingdanList;
            _updateDisplay = updateDisplay;
        }

        // 原来散落在 Form1 各事件里的逻辑，集中到这里
        public void OnMarketPriceChanged(Xiaohaoliang xhl, decimal newPrice)
        {
            xhl.市场价 = newPrice;
            _repo.UpdateMarketPriceByCode(xhl.消耗量编码, newPrice);

            // 同步内存里所有同编码材料
            var same = _qingdanList
                .SelectMany(q => q.定额列表)
                .SelectMany(d => d.消耗量列表)
                .Where(x => x.消耗量编码 == xhl.消耗量编码);

            foreach (var x in same) x.市场价 = newPrice;

            // 全量重算并保存
            foreach (var qd in _qingdanList)
            {
                _calcService.RecalculateQingdan(qd);
                _repo.SaveTree(qd);
            }

            _updateDisplay(Form1.DisplayType.Qingdan);
        }

        public void OnQingdanWorkAmountChanged(Qingdan qd)
        {
            foreach (var dg in qd.定额列表)
                dg.定额工程量 = qd.工程量;

            _calcService.RecalculateQingdan(qd);
            _repo.SaveTree(qd);

            _updateDisplay(Form1.DisplayType.Dinge);
            _updateDisplay(Form1.DisplayType.Xiaohaoliang);
            _updateDisplay(Form1.DisplayType.Qingdan);
        }

        public void OnXiaohaoliangHanliangChanged(Xiaohaoliang xhl, decimal newHanliang)
        {
            // 1. 更新内存
            xhl.含量 = newHanliang;

            // 2. 只找到这条消耗量所属的清单，而不是全量
            var ownerQd = _qingdanList
                .FirstOrDefault(q => q.定额列表
                    .Any(d => d.消耗量列表
                        .Any(x => x.ID号 == xhl.ID号
                               && x.消耗量编码 == xhl.消耗量编码)));

            if (ownerQd == null) return;

            // 3. 只重算、只保存这一条清单
            _calcService.RecalculateQingdan(ownerQd);
            _repo.SaveTree(ownerQd);

            // 4. 刷新 UI
            _updateDisplay(Form1.DisplayType.Qingdan);
            _updateDisplay(Form1.DisplayType.Dinge);
            _updateDisplay(Form1.DisplayType.Xiaohaoliang);
        }

        // QingdanPresenter.cs
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
