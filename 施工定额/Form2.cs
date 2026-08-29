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
        private readonly IImportService _importService;
        private readonly IAppCache _cache;

        public event Action? DataImported;

        public Form2(string targetQingdanCode, string? qingdanCategory = null)
            : this(targetQingdanCode,
                   new ImportService(AppConfig.SystemDbConn, AppConfig.UserDbConn),
                   AppCache.Instance,
                   qingdanCategory)
        {
        }

        public Form2(string targetQingdanCode, IImportService importService, IAppCache cache,
            string? qingdanCategory = null)
        {
            InitializeComponent();
            UiTheme.ApplyTo(this);
            comboBox2.SelectedIndex = 0;
            _targetQingdanCode = targetQingdanCode;
            _qingdanCategory = QingdanCategory.Normalize(qingdanCategory);
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
                node.Tag = cat.分类编码;
                targetTreeView.Nodes.Add(node);
                AddChildNodes(node, cat.Children);
            }

            targetTreeView.EndUpdate();
        }

        private void AddChildNodes(TreeNode parentNode, List<CategoryItem> children)
        {
            foreach (var cat in children)
            {
                TreeNode childNode = new TreeNode();
                childNode.Text = cat.分类名称;
                childNode.Tag = cat.分类编码;
                parentNode.Nodes.Add(childNode);
                if (cat.Children != null && cat.Children.Count > 0)
                    AddChildNodes(childNode, cat.Children);
            }
        }

        private void LoadAndDisplayQingdanTree()
        {
            var roots = _cache.GetQingdanCategoryTree();
            DisplayCategoryTree(treeView1, roots);
        }

        private void LoadAndDisplayDingeTree()
        {
            var roots = _cache.GetDingeCategoryTree();
            DisplayCategoryTree(treeView2, roots);
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            LoadAndDisplayQingdanTree();
            LoadAndDisplayDingeTree();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag == null) return;
            string code = e.Node.Tag.ToString() ?? "";
            dataGridView1.DataSource = _importService.QueryQingdanByCategory(code);
        }

        private void treeView2_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag == null) return;
            string code = e.Node.Tag.ToString() ?? "";
            // 定额列表绑定在设计器/其它逻辑中处理
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex == 0)
                LoadAndDisplayQingdanTree();
            else
                LoadAndDisplayDingeTree();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // 导入选中清单
            if (dataGridView1.SelectedRows.Count == 0)
            {
                ErrorHandler.ShowBusiness("请先选择要导入的清单。");
                return;
            }

            var ids = new List<int>();
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                if (row.Cells["ID号"]?.Value != null && int.TryParse(row.Cells["ID号"].Value.ToString(), out int id))
                    ids.Add(id);
            }

            if (ids.Count == 0)
            {
                ErrorHandler.ShowBusiness("未能读取选中清单的 ID。");
                return;
            }

            try
            {
                foreach (var id in ids)
                    _importService.ImportQingdan(id, _targetQingdanCode, _qingdanCategory);
                DataImported?.Invoke();
                ErrorHandler.ShowBusiness("导入完成。");
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(ex, "导入失败");
            }
        }
    }
}
