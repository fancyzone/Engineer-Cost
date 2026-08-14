namespace 施工定额.Entity
{
    /// <summary>
    /// 取费费率配置。所有费率均为小数形式（如 0.08 表示 8%）。
    /// 可通过 appsettings.json 的 FeeSettings 节点覆盖默认值。
    /// </summary>
    public class FeeRateSettings
    {
        /// <summary>管理费计算基数：DirectCost=直接费，Labor=人工费</summary>
        public string OverheadBase { get; set; } = "DirectCost";

        /// <summary>管理费率（默认 8%）</summary>
        public decimal OverheadRate { get; set; } = 0.08m;

        /// <summary>利润率，基数为（直接费 + 管理费）（默认 5%）</summary>
        public decimal ProfitRate { get; set; } = 0.05m;

        /// <summary>规费率，基数为人工费（默认 0，按需配置）</summary>
        public decimal StatutoryFeeRate { get; set; } = 0m;

        /// <summary>增值税率（默认 9%，一般计税法）</summary>
        public decimal VatRate { get; set; } = 0.09m;

        /// <summary>
        /// 综合合价是否包含规费。
        /// false：综合合价 = 直接费 + 管理费 + 利润（清单综合单价常见构成）
        /// true：综合合价再加规费
        /// </summary>
        public bool IncludeStatutoryInUnitPrice { get; set; } = false;
    }
}
