using System.ComponentModel;
using System.Data;
using 施工定额.Entity;
using 施工定额.Helper;
using 施工定额.UI;
namespace 施工定额
{
    public partial class Form1 : Form
    {
        private BindingList<Qingdan> myMemoryQingdanBindingList = new BindingList<Qingdan>();
        private BindingList<Dinge> _dingeBindingList = new BindingList<Dinge>();
        private BindingList<Xiaohaoliang> _xhlBindingList = new BindingList<Xiaohaoliang>();

        // 只持有服务和仓储的引用，不直接碰数据库和计算逻辑
        private readonly QingdanRepository _repo;
        private readonly CostCalculationService _calcService;
        private readonly QingdanPresenter _qingdanPresenter;
        private readonly SummaryPresenter _summaryPresenter;

        public enum DisplayType { Qingdan, Dinge, Xiaohaoliang }
        private ContextMenuBuilder _menuBuilder;

        // Form1.cs
        public void ReloadAndRecalculateEverything()
        {
            _qingdanPresenter.ReloadAll();  // 数据交给 Presenter

            // UI 层自己负责：校验选中状态
            var stillExists = myMemoryQingdanBindingList.Any(
                q => q.清单编码 == SelectionState.Instance.SelectedQingdanCode);
            if (!stillExists)
                SelectionState.Instance.SelectQingdan("");

            // UI 层自己负责：刷新显示
            UpdateDisplay(DisplayType.Qingdan);
            UpdateDisplay(DisplayType.Dinge);
            UpdateDisplay(DisplayType.Xiaohaoliang);
        }

        public Form1()
        {
            InitializeComponent();
            _repo = new QingdanRepository(AppConfig.UserDbConn);
            _calcService = new CostCalculationService();

            _menuBuilder = new ContextMenuBuilder(
                _repo,
                () => myMemoryQingdanBindingList,  // ← lambda，调用时才求值
                UpdateDisplay, ReloadAndRecalculateEverything);
            dataGridView1.ContextMenuStrip = _menuBuilder.BuildQingdanMenu(dataGridView1);
            // 后续加定额菜单：DataGridView_dinge.ContextMenuStrip = _menuBuilder.BuildDingeMenu(DataGridView_dinge);

            _qingdanPresenter = new QingdanPresenter(_repo, _calcService, myMemoryQingdanBindingList, UpdateDisplay);
            _summaryPresenter = new SummaryPresenter(myMemoryQingdanBindingList, _repo);
            // 订阅 AppState 事件，替代原来到处读 ValueStorage 的做法
            SelectionState.Instance.QingdanSelectionChanged += OnQingdanSelectionChanged;
            SelectionState.Instance.DingeSelectionChanged += OnDingeSelectionChanged;
        }
        // 清单选中变更 → 刷新定额层 + 消耗量层
        private void OnQingdanSelectionChanged(object sender, string code)
        {
            UpdateDisplay(DisplayType.Dinge);
            UpdateDisplay(DisplayType.Xiaohaoliang);
        }

        // 定额选中变更 → 只刷新消耗量层
        private void OnDingeSelectionChanged(object sender, (string code, string id) _)
        {
            UpdateDisplay(DisplayType.Xiaohaoliang);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            // 先建列结构（只做一次）
            InitializeGridColumns();
            ReloadAndRecalculateEverything();
        }
        private void InitializeGridColumns()
        {
            GridManager.BindOnce(dataGridView1, myMemoryQingdanBindingList, GridColumns.Qingdan);
            dataGridView1.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "btnViewImage",
                HeaderText = "图片",
                Text = "查看",
                UseColumnTextForButtonValue = true,
                Width = 60
            });

            GridManager.BindOnce(DataGridView_dinge, _dingeBindingList, GridColumns.Dinge);
            GridManager.BindOnce(dataGridView2, _xhlBindingList, GridColumns.Xiaohaoliang);
        }
        // 更新 dataGridView 的显示
        public void UpdateDisplay(DisplayType type)
        {
            switch (type)
            {
                case DisplayType.Qingdan:
                    // BindingList 已绑定，数据变化会自动通知表格
                    // 什么都不用做，或者只做滚动定位等纯UI操作
                    break;

                case DisplayType.Dinge:
                    {
                        DataGridView_dinge.CellValueChanged -= DataGridView_dinge_CellValueChanged;
                        DataGridView_dinge.CellClick -= DataGridView_dinge_CellClick;

                        _dingeBindingList.Clear();
                        var currentQd = myMemoryQingdanBindingList
                            .FirstOrDefault(q => q.清单编码 == SelectionState.Instance.SelectedQingdanCode);
                        if (currentQd != null)
                            foreach (var d in currentQd.定额列表)
                                _dingeBindingList.Add(d);

                        DataGridView_dinge.CellValueChanged += DataGridView_dinge_CellValueChanged;
                        DataGridView_dinge.CellClick += DataGridView_dinge_CellClick;
                        break;
                    }

                case DisplayType.Xiaohaoliang:
                    {
                        dataGridView2.CellValueChanged -= dataGridView2_CellValueChanged;

                        _xhlBindingList.Clear();
                        var currentQd = myMemoryQingdanBindingList
                            .FirstOrDefault(q => q.清单编码 == SelectionState.Instance.SelectedQingdanCode);
                        var currentDg = currentQd?.定额列表.FirstOrDefault(
                            d => d.定额编码 == SelectionState.Instance.SelectedDingeCode
                              && d.ID号 == SelectionState.Instance.SelectedDingeID);
                        if (currentDg != null)
                            foreach (var x in currentDg.消耗量列表)
                                _xhlBindingList.Add(x);

                        dataGridView2.CellValueChanged += dataGridView2_CellValueChanged;
                        break;
                    }
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;
            string 清单编码 = dataGridView1.Rows[e.RowIndex].Cells["清单编码"].Value?.ToString() ?? "";
            // SelectQingdan 内部会自动清空定额选中，并触发事件通知刷新
            // 不再需要手动写三行 + 手动调 UpdateDisplay
            SelectionState.Instance.SelectQingdan(清单编码);
        }
        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // 1. 过滤掉点击列头（RowIndex == -1）的情况
            if (e.RowIndex == -1) return;

            // 2. 获取当前双击行对应的清单编码，并记录到全局存储中
            // 假设你的 DataGridView 列名叫 "清单编码"，如果不是，请修改为实际的列名或索引（如 Cells[0]）
            string qingdanCode = dataGridView1.Rows[e.RowIndex].Cells["清单编码"].Value?.ToString() ?? "";

            if (string.IsNullOrEmpty(qingdanCode))
            {
                MessageBox.Show("当前选中的清单编码为空，无法打开定额库。");
                return;
            }

            // 3. 实例化弹窗 Form2
            Form2 f2 = new Form2(qingdanCode);

            // 4. ⚡【核心：问题3优化】订阅 Form2 的数据导入成功事件
            f2.DataImported += () =>
            {
                if (IsDisposed) return;
                // 确保即使在复杂的异步环境下，也能安全地在主线程上刷新 UI
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(ReloadAndRecalculateEverything));
                }
                else
                {
                    ReloadAndRecalculateEverything();
                }
            };

            // 5. 以模态对话框形式打开 Form2
            // 使用 ShowDialog 保证用户必须处理完弹窗才能回到主界面
            f2.Show();
        }
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {

        }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dataGridView1.Columns[e.ColumnIndex].Name != "btnViewImage") return;

            string code = dataGridView1.Rows[e.RowIndex].Cells["清单编码"].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(code)) return;

            // 图片文件夹：exe 所在目录下，以清单编码为名的子文件夹
            string imageFolder = Path.Combine(AppContext.BaseDirectory, code);
            if (!Directory.Exists(imageFolder))
            {
                MessageBox.Show($"未找到图片文件夹：{imageFolder}");
                return;
            }

            var supportedExt = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            var imageFiles = Directory.GetFiles(imageFolder)
                .Where(f => supportedExt.Contains(Path.GetExtension(f).ToLower()))
                .ToList();

            if (imageFiles.Count == 0)
            {
                MessageBox.Show("该清单文件夹下没有图片。");
                return;
            }

            new ImageViewerForm(code, imageFiles).Show();
        }
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedTabName = tabControl1.TabPages[tabControl1.SelectedIndex].Name;
            if (selectedTabName == "tabRenCaiJi")//人材机汇总界面
            {
                dataGridView3.DataSource = _summaryPresenter.GetRenCaiJiSummaryFromMemory("");
            }
            if (selectedTabName == "tabCostSummary")//造价汇总界面
                dataGridView4.DataSource = _summaryPresenter.GetCostSummaryData();
        }
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            dataGridView3.DataSource =
                   _summaryPresenter.GetRenCaiJiSummaryFromMemory(e.Node.Text);
        }

        private void dataGridView2_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            dataGridView2.CommitEdit(DataGridViewDataErrorContexts.Commit);

            var source = dataGridView2.DataSource as BindingList<Xiaohaoliang>;
            if (source == null || e.RowIndex >= source.Count) return;
            var xhl = source[e.RowIndex];

            var colName = dataGridView2.Columns[e.ColumnIndex].Name;
            var cellValue = dataGridView2.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

            switch (colName)
            {
                case "市场价":
                    if (decimal.TryParse(cellValue?.ToString(), out var p))
                        _qingdanPresenter.OnMarketPriceChanged(xhl, p);
                    return;

                case "含量":
                    if (decimal.TryParse(cellValue?.ToString(), out var c))
                        _qingdanPresenter.OnXiaohaoliangHanliangChanged(xhl, c);
                    return;

                default:
                    return;
            }
        }
        private void DataGridView_dinge_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;

            DataGridView_dinge.CommitEdit(DataGridViewDataErrorContexts.Commit);
            var currentQd = myMemoryQingdanBindingList
                .FirstOrDefault(q => q.清单编码 == SelectionState.Instance.SelectedQingdanCode);
            if (currentQd == null) return;

            DataGridView_dinge.CellValueChanged -= DataGridView_dinge_CellValueChanged;

            try
            {
                _calcService.RecalculateQingdan(currentQd);
                _repo.SaveTree(currentQd);

                // ✅ 关键修复：延迟到当前事件栈结束后再刷新
                this.BeginInvoke(new Action(() =>
                {
                    UpdateDisplay(DisplayType.Qingdan);
                    UpdateDisplay(DisplayType.Dinge);
                    UpdateDisplay(DisplayType.Xiaohaoliang);
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                // ✅ finally 也要延迟，否则事件重挂时机不对
                this.BeginInvoke(new Action(() =>
                {
                    if (IsDisposed) return;
                    DataGridView_dinge.CellValueChanged += DataGridView_dinge_CellValueChanged;
                }));
            }
        }
        private void DataGridView_dinge_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;
            string 定额编码 = DataGridView_dinge.Rows[e.RowIndex].Cells["定额编码"].Value?.ToString() ?? "";
            string ID号 = DataGridView_dinge.Rows[e.RowIndex].Cells["ID号"].Value?.ToString() ?? "";
            // SelectDinge 内部触发事件，自动刷新消耗量层
            SelectionState.Instance.SelectDinge(定额编码, ID号);
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var changedQd = myMemoryQingdanBindingList.ElementAtOrDefault(e.RowIndex);
            if (changedQd == null) return;

            string colName = dataGridView1.Columns[e.ColumnIndex].Name;

            if (colName == "工程量")
            {
                // SaveTree 已经在 OnQingdanWorkAmountChanged 内部完成，不需要再调用
                _qingdanPresenter.OnQingdanWorkAmountChanged(changedQd);
                return; // ← 直接返回，避免走到下面的 SaveTree
            }

            // 修改的是清单名称、项目特征等其他字段，才在这里保存
            _repo.SaveTree(changedQd);
            UpdateDisplay(DisplayType.Qingdan);
        }
    }
}