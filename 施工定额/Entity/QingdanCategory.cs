namespace 施工定额.Entity
{
    /// <summary>
    /// 清单项目类别（文本存储：分部分项 / 措施项目 / 其他项目）。
    /// </summary>
    public static class QingdanCategory
    {
        public const string 分部分项 = "分部分项";
        public const string 措施项目 = "措施项目";
        public const string 其他项目 = "其他项目";

        public static string Normalize(string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return 分部分项;

            var c = category.Trim();
            // 兼容早期数字编码
            if (c is "0") return 分部分项;
            if (c is "1" or "2") return 措施项目;
            if (c is "3") return 其他项目;

            if (c is 分部分项 or 措施项目 or 其他项目)
                return c;

            if (c.Contains("措施")) return 措施项目;
            if (c.Contains("其他")) return 其他项目;
            return 分部分项;
        }

        public static bool IsMeasure(string? category) =>
            Normalize(category) == 措施项目;

        public static bool IsFenBu(string? category) =>
            Normalize(category) == 分部分项;
    }
}
