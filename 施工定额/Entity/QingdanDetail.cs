namespace 施工定额.Entity
{
    /// <summary>
    /// 系统定额库中的清单参考明细（只读参考数据）。
    /// </summary>
    public class QingdanDetail
    {
        public int 分类ID { get; set; }
        public string 清单编码 { get; set; } = "";
        public string 清单名称 { get; set; } = "";
        public string 项目特征 { get; set; } = "";
        public string 单位 { get; set; } = "";
        public string 工程量计算规则 { get; set; } = "";
        public string 工作内容 { get; set; } = "";
    }
}
