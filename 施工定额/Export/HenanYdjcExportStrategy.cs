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
    /// 注意：这是最小可用版本。程序目前的计算引擎（CostCalculationService）
    /// 只算到"清单综合合价 = Σ定额合价"，并不区分人工费/材料费/机械费/
    /// 管理费/利润/规费/税金这些细项，所以：
    ///   - 人工费/材料费/机械费：按消耗量的"消耗量类别"粗略聚合得出，
    ///     基本能反映真实构成。
    ///   - 管理费/利润/规费/税金：目前没有对应的计算逻辑，先输出 0，
    ///     标记为 TODO。如果需要精确，应在 CostCalculationService 里
    ///     补齐费率计算，而不是在导出这一层硬编。
    /// </summary>
    public class HenanYdjcExportStrategy : IYdjcExportStrategy
    {
        public string StandardName => "DBJ 41/T087-2024";
        public int ValuationMethod => 0; // 清单计价
        public int TaxModel => 0;        // 一般计税法

        private const string DefaultChargeId = "CH001";

        // ── 小数位数配置（对应你程序里实际使用的精度）───────────────
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

        // ── 子目单价计算程序（固定套用标准条文说明里的示例模板）──────
        public XElement BuildChargeTables()
        {
            // 简化版：只保留人工费/材料费/机械费/管理费/利润/规费/风险费/综合单价
            // 管理费、利润费率先固定写 0，等有真实费率数据再补
            var charges = new XElement("Charges",
                new XAttribute("ChargeID", DefaultChargeId),
                new XAttribute("Name", "清单综合单价计算程序（简化版）"),
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
                    new XAttribute("Name", "管理费"), // TODO: 目前费率固定为 0
                    new XAttribute("CalcVariable", "E"),
                    new XAttribute("Formula", "0"),
                    new XAttribute("KindCode", "1006"),
                    new XAttribute("Decimal", 2)),
                new XElement("ChargeItem",
                    new XAttribute("Name", "利润"), // TODO: 目前费率固定为 0
                    new XAttribute("CalcVariable", "F"),
                    new XAttribute("Formula", "0"),
                    new XAttribute("KindCode", "1006"),
                    new XAttribute("Decimal", 2)),
                new XElement("ChargeItem",
                    new XAttribute("Name", "综合单价"),
                    new XAttribute("Formula", "A+B+C+E+F"),
                    new XAttribute("KindCode", "1001"),
                    new XAttribute("Decimal", 2)));

            return new XElement("ChargeTables", charges);
        }

        // ── 清单 → ListProjects（6.5.4）─────────────────────────
        public XElement MapListProjects(Qingdan qd)
        {
            var el = new XElement("ListProjects",
                new XAttribute("Code", qd.清单编码 ?? ""),
                new XAttribute("Name", qd.清单名称 ?? ""),
                new XAttribute("Attr", qd.项目特征 ?? ""),
                new XAttribute("Unit", qd.单位 ?? ""),
                new XAttribute("Quantity", D(qd.工程量)),
                new XAttribute("Price", D(qd.综合单价)),
                new XAttribute("CalcType", 0), // 由定额子目汇总计算得出
                new XAttribute("Total", D(qd.综合合价)));

            el.Add(BuildCosts(qd.定额列表.SelectMany(d => d.消耗量列表).ToList()));

            foreach (var dg in qd.定额列表)
                el.Add(MapNorm(dg));

            return el;
        }

        // ── 定额 → Norm（6.5.5）─────────────────────────────────
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

            el.Add(BuildCosts(dg.消耗量列表));
            el.Add(MapResElements(dg.消耗量列表));

            return el;
        }

        // ── 消耗量列表 → ResElements（6.5.6）────────────────────
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
                    new XAttribute("QtType", 0), // 按消耗量计算
                    new XAttribute("NoCost", false)));
            }
            return el;
        }

        // ── 工料机汇总明细 → ResourceItem（6.8.2）───────────────
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

        // ── 内部辅助方法 ─────────────────────────────────────────

        /// <summary>
        /// 按标准 6.5.2 生成 Costs 元素。目前只精确拆出人工费/材料费/机械费，
        /// 其余（管理费/利润/规费/税金等）固定为 0（TODO：待补充费率计算）。
        /// </summary>
        private XElement BuildCosts(List<Xiaohaoliang> xhlList)
        {
            decimal labor = xhlList.Where(x => x.消耗量类别 == "人").Sum(x => x.市场价合计);
            decimal material = xhlList.Where(x => x.消耗量类别 == "材").Sum(x => x.市场价合计);
            decimal machine = xhlList.Where(x => x.消耗量类别 == "机").Sum(x => x.市场价合计);

            return new XElement("Costs",
                new XAttribute("Labor", D(labor)),
                new XAttribute("Material", D(material)),
                new XAttribute("MainMaterial", D(0)),
                new XAttribute("Equipment", D(0)),
                new XAttribute("MainMaterialEquipment", D(0)),
                new XAttribute("Machine", D(machine)),
                new XAttribute("Overhead", D(0)),   // TODO
                new XAttribute("Profit", D(0)),     // TODO
                new XAttribute("Appraisal", D(0)),
                new XAttribute("LaborQuantity", D(0)));
        }

        /// <summary>消耗量类别 → 附录 A.3 工料机类型编码</summary>
        private static int KindOf(string category) => category switch
        {
            "人" => 1,
            "材" => 2,
            "机" => 3,
            _ => 0
        };

        /// <summary>
        /// 用消耗量编码生成稳定的 ResID（同一份导出内，同编码始终得到同一个 ID，
        /// ResElementItem 和 ResourceItem 之间靠这个关联）。
        /// </summary>
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
