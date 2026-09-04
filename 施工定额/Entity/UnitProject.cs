namespace 施工定额.Entity
{
    /// <summary>单位工程：工程结构节点，一条清单只属于一个单位工程。</summary>
    public class UnitProject
    {
        public const string DefaultCode = "DW001";
        public const string DefaultName = "默认单位工程";

        public string 编码 { get; set; } = "";
        public string 名称 { get; set; } = "";
        public int 排序 { get; set; }
    }
}
