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
            // 1. 同步内存里所有同编码材料的市场价，并收集真正受影响的清单
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

            // 2. 写库：按编码全局更新市场价（跨清单同价语义）
            _repo.UpdateMarketPriceByCode(xhl.消耗量编码, newPrice);

            // 3. 只重算并保存受影响的清单（避免全量 SaveTree）
            foreach (var qd in affectedQingdan)
            {
                _calcService.RecalculateQingdan(qd);
                _repo.SaveTree(qd);
            }

            // 4. 刷新界面（清单合价变化；当前定额/消耗量合计也可能变化）
            _updateDisplay(Form1.DisplayType.Qingdan);
            _updateDisplay(Form1.DisplayType.Dinge);
            _updateDisplay(Form1.DisplayType.Xiaohaoliang);
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
                        .Any(x => x.定额ID == xhl.定额ID
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
