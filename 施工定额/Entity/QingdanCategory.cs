namespace 施工定额.Entity
{
    /// <summary>
    /// 清单项目类别（分部分项 / 单价措施 / 总价措施 同表存放，便于汇总）。
    /// </summary>
    public static class QingdanCategory
    {
        public const int 分部分项 = 0;
        public const int 单价措施 = 1;
        public const int 总价措施 = 2;

        public static string ToDisplayName(int category) => category switch
        {
            单价措施 => "单价措施",
            总价措施 => "总价措施",
            _ => "分部分项"
        };

        public static bool IsMeasure(int category) =>
            category == 单价措施 || category == 总价措施;
    }
}
