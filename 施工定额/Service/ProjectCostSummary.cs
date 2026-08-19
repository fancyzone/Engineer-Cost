namespace 施工定额.Service
{
    /// <summary>
    /// 项目级造价汇总结果
    /// </summary>
    public class ProjectCostSummary
    {
        public decimal 分部分项合价 { get; set; }
        public decimal 措施项目合价 { get; set; }
        public decimal 其他项目合价 { get; set; }
        public decimal 人工费 { get; set; }
        public decimal 材料费 { get; set; }
        public decimal 机械费 { get; set; }
        public decimal 管理费 { get; set; }
        public decimal 利润 { get; set; }
        public decimal 规费 { get; set; }
        public decimal 税金 { get; set; }
        public decimal 不含税总价 { get; set; }
        public decimal 含税总价 { get; set; }
    }
}
