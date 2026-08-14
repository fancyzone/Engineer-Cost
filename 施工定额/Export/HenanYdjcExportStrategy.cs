using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using 施工定额.Entity;

namespace 施工定额.Export
{
    /// <summary>
    /// 河南省《建设工程工程造价成果数据交换标准》（DBJ 41/T087-2024）导出策略。
    ///
    /// 费用构成来自 CostCalculationService 计算的 CostBreakdown：
    ///   - 人工费/材料费/机械费：按消耗量类别汇总
    ///   - 管理费/利润/规费：按 FeeSettings 费率计算
    /// </summary>
    public class HenanYdjcExportStrategy : IYdjcExportStrategy
    {
        public string StandardName => "DBJ 41/T087-2024";
        public int ValuationMethod => 0; // 清单计价
        public int TaxModel => 0;        // 一般计税法

        private const string DefaultChargeId = "CH001";

        public XElement BuildDecimalConfig()
        {
            return new XElement("Decimal",
                new XAttribute("ResourcePrice", 2),
                new XAttribute("ConcretePrice", 2),
                new XAttribute("ResourceQuantity", 4),
                new XAttribute("NormPrice", 2),
                new XAttribute("NormWastage", 4),
                new XAttribute("NormCompositive", 2),
                new XAttribute("Quantity", 3),
                new XAttribute("ListCompositive", 2),
                new XAttribute("Appraisals", 2));
        }

        public XElement BuildChargeTables()
        {
            var rates = Helper.AppConfig.FeeRates;
            string overheadFormula = string.Equals(rates.OverheadBase, "Labor", StringComparison.OrdinalIgnoreCase)
                ? $"A*{rates.OverheadRate.ToString(CultureInfo.InvariantCulture)}"
                : $"(A+B+C)*{rates.OverheadRate.ToString(CultureInfo.InvariantCulture)}";
            string profitFormula = $"(A+B+C+E)*{rates.ProfitRate.ToString(CultureInfo.InvariantCulture)}";

            var charges = new XElement("Charges",
                new XAttribute("ChargeID", DefaultChargeId),
                new XAttribute("Name", "清单综合单价计算程序"),
                new XElement("ChargeItem",
                    new XAttribute("Name", "人工费"),
                    new XAttribute("CalcVariable", "A"),
                    new XAttribute("CalcBasis", "DERGF"),
                    new XAttribute("KindCode", "1003"),
                    new XAttribute("Decimal", 2)),
                new XElement("ChargeItem",
                    new XAttribute("Name", "材料费"),
                    new XAttribute("CalcVariable", "B"),
                    new XAttribute("CalcBasis", "DECLF"),
                    new XAttribute("KindCode", "1004"),
                    new XAttribute("Decimal", 2)),
                new XElement("ChargeItem",
                    new XAttribute("Name", "机械费"),
                    new XAttribute("CalcVariable", "C"),
                    new XAttribute("CalcBasis", "DEJXF"),
                    new XAttribute("KindCode", "1005"),
                    new XAttribute("Decimal", 2)),
                new XElement("ChargeItem",
                    new XAttribute("Name", "管理费"),
                    new XAttribute("CalcVariable", "E"),
                    new XAttribute("Formula", overheadFormula),
                    new XAttribute("KindCode", "1006"),
                    new XAttribute("Decimal", 2)),
                new XElement("ChargeItem",
                    new XAttribute("Name", "利润"),
                    new XAttribute("CalcVariable", "F"),
                    new XAttribute("Formula", profitFormula),
                    new XAttribute("KindCode", "1006"),
                    new XAttribute("Decimal", 2)),
                new XElement("ChargeItem",
                    new XAttribute("Name", "综合单价"),
                    new XAttribute("Formula", "A+B+C+E+F"),
                    new XAttribute("KindCode", "1001"),
                    new XAttribute("Decimal", 2)));

            return new XElement("ChargeTables", charges);
        }

        public XElement MapListProjects(Qingdan qd)
        {
            var el = new XElement("ListProjects",
                new XAttribute("Code", qd.清单编码 ?? ""),
                new XAttribute("Name", qd.清单名称 ?? ""),
                new XAttribute("Attr", qd.项目特征 ?? ""),
                new XAttribute("Unit", qd.单位 ?? ""),
                new XAttribute("Quantity", D(qd.工程量)),
                new XAttribute("Price", D(qd.综合单价)),
                new XAttribute("CalcType", 0),
                new XAttribute("Total", D(qd.综合合价)));

            el.Add(BuildCostsFromBreakdown(qd.费用构成));

            foreach (var dg in qd.定额列表)
                el.Add(MapNorm(dg));

            return el;
        }

        public XElement MapNorm(Dinge dg)
        {
            var el = new XElement("Norm",
                new XAttribute("Code", dg.定额编码 ?? ""),
                new XAttribute("Name", dg.定额名称 ?? ""),
                new XAttribute("Unit", dg.定额单位 ?? ""),
                new XAttribute("Quantity", D(dg.定额工程量)),
                new XAttribute("Price", D(dg.定额单价)),
                new XAttribute("Total", D(dg.定额合价)),
                new XAttribute("ChargeID", DefaultChargeId));

            el.Add(BuildCostsFromBreakdown(dg.费用构成));
            el.Add(MapResElements(dg.消耗量列表));

            return el;
        }

        public XElement MapResElements(List<Xiaohaoliang> xhlList)
        {
            var el = new XElement("ResElements");
            foreach (var x in xhlList)
            {
                el.Add(new XElement("ResElementItem",
                    new XAttribute("ResID", ResIdFor(x.消耗量编码)),
                    new XAttribute("Quantity", D(x.含量)),
                    new XAttribute("Quantitys", D(x.数量)),
                    new XAttribute("Price", D(x.市场价)),
                    new XAttribute("Total", D(x.市场价合计)),
                    new XAttribute("QtType", 0),
                    new XAttribute("NoCost", false)));
            }
            return el;
        }

        public XElement MapResourceItem(ResourceAggregate agg)
        {
            return new XElement("ResourceItem",
                new XAttribute("ResID", agg.ResID),
                new XAttribute("ResourceCode", agg.消耗量编码 ?? ""),
                new XAttribute("Name", agg.消耗量名称 ?? ""),
                new XAttribute("Specification", agg.规格型号 ?? ""),
                new XAttribute("Unit", agg.消耗量单位 ?? ""),
                new XAttribute("OrgPrice", D(agg.定额基价)),
                new XAttribute("Price", D(agg.市场价)),
                new XAttribute("Quantity", D(agg.数量合计)),
                new XAttribute("OrgTotal", D(agg.定额价合价)),
                new XAttribute("Total", D(agg.编制价合价)),
                new XAttribute("Kind", KindOf(agg.消耗量类别)));
        }

        private XElement BuildCostsFromBreakdown(CostBreakdown b)
        {
            return new XElement("Costs",
                new XAttribute("Labor", D(b.人工费)),
                new XAttribute("Material", D(b.材料费)),
                new XAttribute("MainMaterial", D(0)),
                new XAttribute("Equipment", D(0)),
                new XAttribute("MainMaterialEquipment", D(0)),
                new XAttribute("Machine", D(b.机械费)),
                new XAttribute("Overhead", D(b.管理费)),
                new XAttribute("Profit", D(b.利润)),
                new XAttribute("Appraisal", D(0)),
                new XAttribute("LaborQuantity", D(0)));
        }

        private static int KindOf(string category) => category switch
        {
            "人" => 1,
            "材" => 2,
            "机" => 3,
            _ => 0
        };

        public static string ResIdFor(string 消耗量编码)
        {
            消耗量编码 ??= "";
            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(消耗量编码));
            return new Guid(bytes).ToString();
        }

        private static string D(decimal value) =>
            value.ToString(CultureInfo.InvariantCulture);
    }
}
