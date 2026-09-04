using System.Data;
using 施工定额.Entity;
using 施工定额.Helper;
using 施工定额.Service;
using 施工定额.UI;

namespace 施工定额
{
    public partial class Form2 : Form
    {
        private readonly string _targetQingdanCode;
        private readonly string _qingdanCategory;
        private readonly string _unitProjectCode;
        private readonly IImportService _importService;
        private readonly IAppCache _cache;

        public event Action? DataImported;

        public Form2(string targetQingdanCode, string? qingdanCategory = null, string? unitProjectCode = null)
            : this(targetQingdanCode,
                   new ImportService(AppConfig.SystemDbConn, AppConfig.UserDbConn),
                   AppCache.Instance,
                   qingdanCategory,
                   unitProjectCode)
        {
        }

        /// <summary>
        /// 可注入构造：便于测试与后续 DI 接入。
        /// </summary>
        public Form2(string targetQingdanCode, IImportService importService, IAppCache cache,
            string? qingdanCategory = null, string? unitProjectCode = null)
        {
            InitializeComponent();
            UiTheme.ApplyTo(this);
            comboBox2.SelectedIndex = 0;
            _targetQingdanCode = targetQingdanCode;
            _qingdanCategory = QingdanCategory.Normalize(qingdanCategory);
            _unitProjectCode = string.IsNullOrWhiteSpace(unitProjectCode)
                ? UnitProject.DefaultCode
                : unitProjectCode.Trim();
            _importService = importService;
            _cache = cache;
        }

        private void DisplayCategoryTree(TreeView targetTreeView, List<CategoryItem> rootCategories)
        {
            targetTreeView.BeginUpdate();
            targetTreeView.Nodes.Clear();

            foreach (var cat in rootCategories)
            {
                TreeNode node = new TreeNode();
                node.Text = cat.分类名称;
                node.Tag = cat.分类ID;
                AppendChildNodes(node, cat);
                targetTreeView.Nodes.Add(node);
            }
            targetTreeView.EndUpdate();
        }

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

        private void GetAllNodeIds(TreeNode node, List<int> ids)
        {
            if (node.Tag != null)
                ids.Add((int)node.Tag);

            foreach (TreeNode child in node.Nodes)
                GetAllNodeIds(child, ids);
        }

        private void LoadAndDisplayQingdanTree()
        {
            DisplayCategoryTree(treeView1, _cache.QingdanCategories.ToList());
            treeView1.ExpandAll();
        }

        private void LoadAndDisplayDingeTree()
        {
            DisplayCategoryTree(treeView2, _cache.DingeCategories.ToList());
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
                LoadAndDisplayQingdanTree();
            else if (selectedIndex == 1 && treeView2.Nodes.Count == 0)
                LoadAndDisplayDingeTree();
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;

            string code = dataGridView1.Rows[e.RowIndex].Cells["清单编码"].Value?.ToString() ?? "";
            string name = dataGridView1.Rows[e.RowIndex].Cells["清单名称"].Value?.ToString() ?? "";
            string feature = dataGridView1.Rows[e.RowIndex].Cells["项目特征"].Value?.ToString() ?? "";
            string unit = dataGridView1.Rows[e.RowIndex].Cells["单位"].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(code)) return;

            try
            {
                _importService.ImportQingdan(code, name, feature, unit, _qingdanCategory, _unitProjectCode);
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(ex, "导入清单失败");
                return;
            }

            DataImported?.Invoke();
            this.Close();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null || e.Node.Tag == null) return;

            List<int> ids = new List<int>();
            GetAllNodeIds(e.Node, ids);

            if (ids.Count == 0)
            {
                dataGridView1.DataSource = null;
                return;
            }

            dataGridView1.DataSource = _cache.GetQingdanDetailsByCategoryIds(ids).ToList();
        }

        private void dataGridView2_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;
            if (string.IsNullOrEmpty(_targetQingdanCode))
            {
                ErrorHandler.ShowBusiness("请先选择一条清单，再导入定额。");
                return;
            }
            string sysId = dataGridView2.Rows[e.RowIndex].Cells["ID号"].Value?.ToString() ?? "";
            string code = dataGridView2.Rows[e.RowIndex].Cells["定额编码"].Value?.ToString() ?? "";
            string name = dataGridView2.Rows[e.RowIndex].Cells["定额名称"].Value?.ToString() ?? "";
            string unit = dataGridView2.Rows[e.RowIndex].Cells["定额单位"].Value?.ToString() ?? "";

            try
            {
                _importService.ImportDinge(_targetQingdanCode, sysId, code, name, unit);
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(ex, "导入定额失败");
                return;
            }

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
            if (ids.Count == 0)
            {
                dataGridView2.DataSource = null;
                return;
            }

            dataGridView2.DataSource = _cache.GetDingeByCategoryIds(ids).ToList();
        }
    }
}
