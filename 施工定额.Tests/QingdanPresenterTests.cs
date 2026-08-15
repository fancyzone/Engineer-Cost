using System.ComponentModel;
using 施工定额;
using 施工定额.Entity;
using 施工定额.UI;
using Xunit;

namespace 施工定额.Tests
{
    public class QingdanPresenterTests
    {
        private sealed class FakeRepo : IQingdanRepository
        {
            public List<Qingdan> Tree { get; set; } = new();
            public int SaveTreeCalls { get; private set; }
            public int SaveHeaderCalls { get; private set; }
            public int SaveDingeCalls { get; private set; }

            public List<Qingdan> LoadTree() => Tree.ToList();
            public void SaveTree(Qingdan qd) { SaveTreeCalls++; }
            public void SaveQingdanHeader(Qingdan qd) { SaveHeaderCalls++; }
            public void SaveDinge(Dinge dg) { SaveDingeCalls++; }
            public void SaveXiaohaoliang(Xiaohaoliang xhl) { }
            public void UpdateMarketPriceByCode(string 消耗量编码, decimal 新市场价) { }
            public void DeleteQingdan(string qingdanCode) { }
        }

        private static Dinge SampleDinge(decimal work = 10m, decimal factor = 1m)
        {
            var dg = new Dinge
            {
                ID号 = "dg1",
                定额编码 = "D-001",
                定额工程量 = work,
                换算系数 = factor,
            };
            dg.消耗量列表.Add(new Xiaohaoliang
            {
                消耗量类别 = "人",
                消耗量编码 = "L1",
                含量 = 2m,
                市场价 = 100m
            });
            return dg;
        }

        [Fact]
        public void OnDingeConversionFactorChanged_UpdatesWorkAmountAndUsesFineSave()
        {
            var repo = new FakeRepo();
            var list = new BindingList<Qingdan>();
            var qd = new Qingdan { 清单编码 = "Q1", 工程量 = 10m };
            var dg = SampleDinge(10m, 1m);
            qd.定额列表.Add(dg);
            list.Add(qd);

            var calc = new CostCalculationService(new FeeRateSettings
            {
                OverheadRate = 0,
                ProfitRate = 0,
                VatRate = 0
            });
            var presenter = new QingdanPresenter(repo, calc, list, _ => { });

            dg.换算系数 = 2m;
            presenter.OnDingeConversionFactorChanged(qd, dg);

            Assert.Equal(20m, dg.定额工程量);
            Assert.Equal(0, repo.SaveTreeCalls);
            Assert.True(repo.SaveHeaderCalls >= 1);
            Assert.True(repo.SaveDingeCalls >= 1);
        }

        [Fact]
        public void OnQingdanWorkAmountChanged_AppliesFactorPerDinge()
        {
            var repo = new FakeRepo();
            var list = new BindingList<Qingdan>();
            var qd = new Qingdan { 清单编码 = "Q1", 工程量 = 5m };
            var dg1 = SampleDinge(5m, 1m);
            var dg2 = SampleDinge(10m, 2m);
            dg2.ID号 = "dg2";
            qd.定额列表.Add(dg1);
            qd.定额列表.Add(dg2);
            list.Add(qd);

            var calc = new CostCalculationService(new FeeRateSettings
            {
                OverheadRate = 0,
                ProfitRate = 0,
                VatRate = 0
            });
            var presenter = new QingdanPresenter(repo, calc, list, _ => { });

            qd.工程量 = 8m;
            presenter.OnQingdanWorkAmountChanged(qd);

            Assert.Equal(8m, dg1.定额工程量);
            Assert.Equal(16m, dg2.定额工程量);
            Assert.Equal(0, repo.SaveTreeCalls);
            Assert.True(repo.SaveDingeCalls >= 2);
        }
    }
}
