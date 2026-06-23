namespace 施工定额.Entity
{
    public class CategoryItem
    {
        public int 分类ID { get; set; }
        public string 分类名称 { get; set; }
        public int 父ID { get; set; }

        // 关键：在内存中直接挂载子节点，免去递归时反复扫描全量数据
        public List<CategoryItem> 子分类列表 { get; set; } = new List<CategoryItem>();
    }
}