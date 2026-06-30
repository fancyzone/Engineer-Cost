using System.Data;
using 施工定额.Entity;
using 施工定额.Helper;
using 施工定额.Service;

namespace 施工定额
{
    public partial class Form2 : Form
    {
        private readonly CostCalculationService _calcService = new CostCalculationService();
        private readonly string _targetQingdanCode;
        public event Action DataImported;
        // 渲染入口
        private void DisplayCategoryTree(TreeView targetTreeView, List<CategoryItem> rootCategories)
        {
            targetTreeView.BeginUpdate(); // 锁定界面，防止闪烁
            targetTreeView.Nodes.Clear();

            foreach (var cat in rootCategories)
            {
                TreeNode node = new TreeNode();
                node.Text = cat.分类名称;
                node.Tag = cat.分类ID; // 依然把ID藏在Tag里，供后续网格联动使用
                // 递归渲染子节点
                AppendChildNodes(node, cat);
                targetTreeView.Nodes.Add(node);
            }
            targetTreeView.EndUpdate(); // 解除锁定
        }

        // 递归辅助方法
        private void AppendChildNodes(TreeNode parentNode, CategoryItem parentCat)
        {
            foreach (var childCat in parentCat.子分类列表)
            {
                TreeNode childNode = new TreeNode();
                childNode.Text = childCat.分类名称;
                childNode.Tag = childCat.分类ID;

                AppendChildNodes(childNode, childCat);
                parentNode.Nodes.Add(childNode);
            }
        }

        /// <summary>
        /// 辅助函数：递归获取某个树节点下的所有子节点ID
        /// </summary>
        private void GetAllNodeIds(TreeNode node, List<int> ids)
        {
            if (node.Tag != null)
            {
                ids.Add((int)node.Tag);
            }

            // 循环遍历子节点
            foreach (TreeNode child in node.Nodes)
            {
                GetAllNodeIds(child, ids);
            }
        }
        private readonly ImportService _importService;
        public Form2(string targetQingdanCode)
        {
            InitializeComponent();
            comboBox2.SelectedIndex = 0;
            _targetQingdanCode = targetQingdanCode;
            _importService = new ImportService(AppConfig.SystemDbConn, AppConfig.UserDbConn);
        }
        private void LoadAndDisplayQingdanTree()
        {
            DisplayCategoryTree(treeView1, AppCache.Instance.QingdanCategories.ToList());
            treeView1.ExpandAll();
        }
        private void LoadAndDisplayDingeTree()
        {
            DisplayCategoryTree(treeView2, AppCache.Instance.DingeCategories.ToList());
            treeView2.ExpandAll();
        }
        private void Form2_Load(object sender, EventArgs e)
        {
            LoadAndDisplayQingdanTree();
            LoadAndDisplayDingeTree();
        }

        private void tabControl1_MouseClick(object sender, MouseEventArgs e)
        {

        }
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = tabControl1.SelectedIndex;

            if (selectedIndex == 0 && treeView1.Nodes.Count == 0)
            {
                LoadAndDisplayQingdanTree();
            }
            else if (selectedIndex == 1 && treeView2.Nodes.Count == 0)
            {
                LoadAndDisplayDingeTree();
            }
        }
        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;

            string code = dataGridView1.Rows[e.RowIndex].Cells["清单编码"].Value?.ToString() ?? "";
            string name = dataGridView1.Rows[e.RowIndex].Cells["清单名称"].Value?.ToString() ?? "";
            string feature = dataGridView1.Rows[e.RowIndex].Cells["项目特征"].Value?.ToString() ?? "";
            string unit = dataGridView1.Rows[e.RowIndex].Cells["单位"].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(code)) return;

            try { _importService.ImportQingdan(code, name, feature, unit); }
            catch (Exception ex) { MessageBox.Show($"导入失败：{ex.Message}"); return; }

            DataImported?.Invoke();
            this.Close();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // 如果点击的节点没有 Tag，或者 Tag 为空，直接返回
            if (e.Node == null || e.Node.Tag == null) return;

            // 1. 收集当前选中的节点，以及它下面所有子节点的 Tag (分类ID)
            List<int> ids = new List<int>();
            GetAllNodeIds(e.Node, ids);

            // ✅ 加这个保护
            if (ids.Count == 0)
            {
                dataGridView1.DataSource = null;
                return;
            }

            // 和定额侧完全对称，纯内存操作
            var idSet = new HashSet<int>(ids);
            var filteredList = AppCache.Instance.QingdanDetails
                                                .Where(q => idSet.Contains(q.分类ID))
                                                .ToList();

            dataGridView1.DataSource = filteredList;

        }

        //定额处理
        private void dataGridView2_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;
            if (string.IsNullOrEmpty(_targetQingdanCode))
            {
                MessageBox.Show("请先选择一条清单，再导入定额。");
                return;
            }
            string sysId = dataGridView2.Rows[e.RowIndex].Cells["ID号"].Value?.ToString() ?? "";
            string code = dataGridView2.Rows[e.RowIndex].Cells["定额编码"].Value?.ToString() ?? "";
            string name = dataGridView2.Rows[e.RowIndex].Cells["定额名称"].Value?.ToString() ?? "";
            string unit = dataGridView2.Rows[e.RowIndex].Cells["定额单位"].Value?.ToString() ?? "";

            try { _importService.ImportDinge(_targetQingdanCode, sysId, code, name, unit); }
            catch (Exception ex) { MessageBox.Show($"导入失败：{ex.Message}"); return; }

            DataImported?.Invoke();
            this.Close();
        }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void treeView2_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;

            List<int> ids = new List<int>();
            GetAllNodeIds(e.Node, ids);
            // ✅ 加保护
            if (ids.Count == 0)
            {
                dataGridView2.DataSource = null;
                return;
            }
            // ✅ 真正彻底的纯内存操作：不查数据库，直接从全局缓存里用 LINQ 过滤
            var idSet = new HashSet<int>(ids);
            var filteredList = AppCache.Instance.DingeDetails
                                                .Where(d => idSet.Contains(d.分类ID))
                                                .ToList();

            dataGridView2.DataSource = filteredList;
        }
    }
}