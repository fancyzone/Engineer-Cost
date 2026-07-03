using System.Xml.Linq;
using 施工定额.Entity;

namespace 施工定额.Export
{
    /// <summary>
    /// 造价成果数据交换标准（.YDJC）导出策略接口。
    /// 不同省份的标准（如河南 DBJ 41/T087-2024）可能在字段、枚举值、
    /// 计价程序等方面存在差异，通过实现本接口来隔离这些差异，
    /// 不需要改动主程序的计算逻辑或数据模型。
    ///
    /// 目前只有河南一份标准的实现（HenanYdjcExportStrategy）。
    /// 等真正遇到河北等其他省份的标准时，再新增一个实现类即可，
    /// 不建议在没有具体标准文本前预先猜测差异点。
    /// </summary>
    public interface IYdjcExportStrategy
    {
        /// <summary>标准名称，对应 ProjectInfo.StandardName，如 "DBJ 41/T087-2024"</summary>
        string StandardName { get; }

        /// <summary>计价类别：0=清单计价；1=定额计价</summary>
        int ValuationMethod { get; }

        /// <summary>计税模式：0=一般计税法；1=简易计税法</summary>
        int TaxModel { get; }

        /// <summary>
        /// 将一条清单映射为 ListProjects 元素（标准 6.5.4）。
        /// </summary>
        XElement MapListProjects(Qingdan qd);

        /// <summary>
        /// 将一条定额映射为 Norm 元素（标准 6.5.5），
        /// 内部会调用 MapResElements 生成子元素 ResElements。
        /// </summary>
        XElement MapNorm(Dinge dg);

        /// <summary>
        /// 将定额下属的消耗量列表映射为 ResElements 元素（标准 6.5.6）。
        /// </summary>
        XElement MapResElements(List<Xiaohaoliang> xhlList);

        /// <summary>
        /// 生成子目单价计算程序表 ChargeTables（标准 6.2.3/6.2.4），
        /// 目前固定输出一套简化的清单计价程序模板。
        /// </summary>
        XElement BuildChargeTables();

        /// <summary>
        /// 将全项目的消耗量按"消耗量编码"去重合并后的一条记录
        /// 映射为 ResourceItem 元素（标准 6.8.2）。
        /// </summary>
        XElement MapResourceItem(ResourceAggregate agg);

        /// <summary>
        /// 生成计算小数位数配置 Decimal 元素（标准 6.2.2），
        /// 固定按程序里实际使用的精度输出。
        /// </summary>
        XElement BuildDecimalConfig();
    }

    /// <summary>
    /// 用于工料机汇总（Resource）的聚合结果：
    /// 同一"消耗量编码"在不同定额、不同清单下的用量会被累加为一条。
    /// </summary>
    public class ResourceAggregate
    {
        public string ResID { get; set; } = "";
        public string 消耗量编码 { get; set; } = "";
        public string 消耗量名称 { get; set; } = "";
        public string 规格型号 { get; set; } = "";
        public string 消耗量单位 { get; set; } = "";
        public string 消耗量类别 { get; set; } = "";
        public decimal 定额基价 { get; set; }
        public decimal 市场价 { get; set; }
        public decimal 数量合计 { get; set; }
        public decimal 定额价合价 { get; set; }
        public decimal 编制价合价 { get; set; }
    }
}
