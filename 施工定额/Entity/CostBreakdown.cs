namespace 施工定额.Entity
{
    /// <summary>
    /// 费用构成明细（运行时计算结果，不持久化到数据库）。
    /// </summary>
    public class CostBreakdown
    {
        public decimal 人工费 { get; set; }
        public decimal 材料费 { get; set; }
        public decimal 机械费 { get; set; }

        /// <summary>直接费 = 人工费 + 材料费 + 机械费</summary>
        public decimal 直接费 => 人工费 + 材料费 + 机械费;

        public decimal 管理费 { get; set; }
        public decimal 利润 { get; set; }
        public decimal 规费 { get; set; }

        /// <summary>不含税合价 = 直接费 + 管理费 + 利润（± 规费，取决于配置）</summary>
        public decimal 不含税合价 { get; set; }

        public decimal 税金 { get; set; }

        /// <summary>含税合价 = 不含税合价 + 税金</summary>
        public decimal 含税合价 => 不含税合价 + 税金;

        public void Reset()
        {
            人工费 = 材料费 = 机械费 = 0;
            管理费 = 利润 = 规费 = 0;
            不含税合价 = 税金 = 0;
        }
    }
}
