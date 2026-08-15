using System.ComponentModel;
using 施工定额.Entity;
using 施工定额.Export;
using 施工定额.Helper;
using 施工定额.Service;
using 施工定额.UI;

namespace 施工定额
{
    public partial class Form1 : Form
    {
        private readonly BindingList<Qingdan> myMemoryQingdanBindingList = new BindingList<Qingdan>();
        private readonly BindingList<Dinge> _dingeBindingList = new BindingList<Dinge>();
        private readonly BindingList<Xiaohaoliang> _xhlBindingList = new BindingList<Xiaohaoliang>();

        private readonly IQingdanRepository _repo;
        private readonly ICostCalculationService _calcService;
        private readonly SelectionState _selection;
        private readonly QingdanPresenter _qingdanPresenter;
        private readonly SummaryPresenter _summaryPresenter;
        private readonly ContextMenuBuilder _menuBuilder;

        /// <summary>刷新子表时抑制 CellValueChanged，避免事件重入。</summary>
        private bool _suppressGridEvents;

        public void ReloadAndRecalculateEverything()
        {
            _qingdanPresenter.ReloadAll();

            var stillExists = myMemoryQingdanBindingList.Any(
                q => q.清单编码 == _selection.SelectedQingdanCode);
            if (!stillExists)
                _selection.SelectQingdan("");

            UpdateDisplay(DisplayType.Qingdan);
            UpdateDisplay(DisplayType.Dinge);
            UpdateDisplay(DisplayType.Xiaohaoliang);
        }

        public Form1()
        {
            InitializeComponent();

            // 组合根：在此处组装依赖，Form 只持有抽象与 Presenter
            _repo = new QingdanRepository(AppConfig.UserDbConn);
            _calcService = new CostCalculationService();
            _selection = new SelectionState();

            _qingdanPresenter = new QingdanPresenter(
                _repo, _calcService, myMemoryQingdanBindingList, UpdateDisplay);
            _summaryPresenter = new SummaryPresenter(myMemoryQingdanBindingList, _calcService);

            _menuBuilder = new ContextMenuBuilder(
                _qingdanPresenter,
                _selection,
                ReloadAndRecalculateEverything);
            dataGridView1.ContextMenuStrip = _menuBuilder.BuildQingdanMenu(dataGridView1);

            _selection.QingdanSelectionChanged += OnQingdanSelectionChanged;
            _selection.DingeSelectionChanged += OnDingeSelectionChanged;
        }

        private void OnQingdanSelectionChanged(object? sender, string code)
        {
            UpdateDisplay(DisplayType.Dinge);
            UpdateDisplay(DisplayType.Xiaohaoliang);
        }

        private void OnDingeSelectionChanged(object? sender, (string code, string id) _)
        {
            UpdateDisplay(DisplayType.Xiaohaoliang);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
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

        /// <summary>
        /// 纯 UI：根据当前选中状态刷新三层表格的 BindingList。
        /// </summary>
        public void UpdateDisplay(DisplayType type)
        {
            switch (type)
            {
                case DisplayType.Qingdan:
                    break;

                case DisplayType.Dinge:
                    {
                        _suppressGridEvents = true;
                        try
                        {
                            var currentQd = myMemoryQingdanBindingList
                                .FirstOrDefault(q => q.清单编码 == _selection.SelectedQingdanCode);
                            _dingeBindingList.ReplaceAll(
                                currentQd?.定额列表 ?? Enumerable.Empty<Dinge>());
                        }
                        finally
                        {
                            _suppressGridEvents = false;
                        }
                        break;
                    }

                case DisplayType.Xiaohaoliang:
                    {
                        _suppressGridEvents = true;
                        try
                        {
                            var currentQd = myMemoryQingdanBindingList
                                .FirstOrDefault(q => q.清单编码 == _selection.SelectedQingdanCode);
                            var currentDg = currentQd?.定额列表.FirstOrDefault(
                                d => d.定额编码 == _selection.SelectedDingeCode
                                  && d.ID号 == _selection.SelectedDingeID);
                            _xhlBindingList.ReplaceAll(
                                currentDg?.消耗量列表 ?? Enumerable.Empty<Xiaohaoliang>());
                        }
                        finally
                        {
                            _suppressGridEvents = false;
                        }
                        break;
                    }
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;
            string 清单编码 = dataGridView1.Rows[e.RowIndex].Cells["清单编码"].Value?.ToString() ?? "";
            _selection.SelectQingdan(清单编码);
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;

            string qingdanCode = dataGridView1.Rows[e.RowIndex].Cells["清单编码"].Value?.ToString() ?? "";

            if (string.IsNullOrEmpty(qingdanCode))
            {
                ErrorHandler.ShowBusiness("当前选中的清单编码为空，无法打开定额库。");
                return;
            }

            Form2 f2 = new Form2(qingdanCode);

            f2.DataImported += () =>
            {
                if (IsDisposed) return;
                if (InvokeRequired)
                    Invoke(new Action(ReloadAndRecalculateEverything));
                else
                    ReloadAndRecalculateEverything();
            };

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

            // 优先 AppData 下的图片目录，其次程序目录
            string imageFolder = Path.Combine(AppConfig.DataDirectory, "images", code);
            if (!Directory.Exists(imageFolder))
                imageFolder = Path.Combine(AppContext.BaseDirectory, code);

            if (!Directory.Exists(imageFolder))
            {
                ErrorHandler.ShowBusiness($"未找到图片文件夹：{imageFolder}");
                return;
            }

            var supportedExt = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            var imageFiles = Directory.GetFiles(imageFolder)
                .Where(f => supportedExt.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            if (imageFiles.Count == 0)
            {
                ErrorHandler.ShowBusiness("该清单文件夹下没有图片。");
                return;
            }

            new ImageViewerForm(code, imageFiles).Show();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedTabName = tabControl1.TabPages[tabControl1.SelectedIndex].Name;
            if (selectedTabName == "tabRenCaiJi")
                dataGridView3.DataSource = _summaryPresenter.GetRenCaiJiSummaryFromMemory("");
            if (selectedTabName == "tabCostSummary")
                dataGridView4.DataSource = _summaryPresenter.GetCostSummaryData();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            dataGridView3.DataSource =
                _summaryPresenter.GetRenCaiJiSummaryFromMemory(e.Node?.Text ?? "");
        }

        private void dataGridView2_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_suppressGridEvents || e.RowIndex < 0) return;
            dataGridView2.CommitEdit(DataGridViewDataErrorContexts.Commit);

            var source = dataGridView2.DataSource as BindingList<Xiaohaoliang>;
            if (source == null || e.RowIndex >= source.Count) return;
            var xhl = source[e.RowIndex];

            var colName = dataGridView2.Columns[e.ColumnIndex].Name;
            var cellValue = dataGridView2.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

            try
            {
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
            catch (Exception ex)
            {
                ErrorHandler.Show(ex);
            }
        }

        private void DataGridView_dinge_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_suppressGridEvents || e.RowIndex == -1) return;

            DataGridView_dinge.CommitEdit(DataGridViewDataErrorContexts.Commit);
            var currentQd = myMemoryQingdanBindingList
                .FirstOrDefault(q => q.清单编码 == _selection.SelectedQingdanCode);
            if (currentQd == null) return;

            _suppressGridEvents = true;
            try
            {
                var colName = DataGridView_dinge.Columns[e.ColumnIndex].Name;
                if (colName == "换算系数")
                {
                    var dg = _dingeBindingList.ElementAtOrDefault(e.RowIndex);
                    if (dg != null)
                        _qingdanPresenter.OnDingeConversionFactorChanged(currentQd, dg);
                    else
                        _qingdanPresenter.OnDingeChanged(currentQd);
                }
                else
                {
                    _qingdanPresenter.OnDingeChanged(currentQd);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(ex);
            }
            finally
            {
                BeginInvoke(new Action(() =>
                {
                    if (!IsDisposed)
                        _suppressGridEvents = false;
                }));
            }
        }

        private void DataGridView_dinge_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;
            string 定额编码 = DataGridView_dinge.Rows[e.RowIndex].Cells["定额编码"].Value?.ToString() ?? "";
            string ID号 = DataGridView_dinge.Rows[e.RowIndex].Cells["ID号"].Value?.ToString() ?? "";
            _selection.SelectDinge(定额编码, ID号);
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var changedQd = myMemoryQingdanBindingList.ElementAtOrDefault(e.RowIndex);
            if (changedQd == null) return;

            string colName = dataGridView1.Columns[e.ColumnIndex].Name;

            try
            {
                if (colName == "工程量")
                {
                    _qingdanPresenter.OnQingdanWorkAmountChanged(changedQd);
                    return;
                }

                _qingdanPresenter.SaveQingdanFields(changedQd);
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(ex);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (myMemoryQingdanBindingList.Count == 0)
            {
                ErrorHandler.ShowBusiness("当前没有可导出的清单数据。");
                return;
            }

            using var infoForm = new ExportProjectInfoForm();
            if (infoForm.ShowDialog(this) != DialogResult.OK)
                return;

            using var sfd = new SaveFileDialog
            {
                Title = "导出 YDJC 文件",
                Filter = "YDJC 文件 (*.YDJC)|*.YDJC|所有文件 (*.*)|*.*",
                FileName = $"{infoForm.ProjectName}.YDJC",
                DefaultExt = "YDJC",
                AddExtension = true
            };

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                Cursor = Cursors.WaitCursor;
                IYdjcExportStrategy strategy = new HenanYdjcExportStrategy();
                var exportService = new YdjcExportService(strategy, _calcService);

                var info = new YdjcProjectInfo
                {
                    ProjectName = infoForm.ProjectName,
                    Owner = infoForm.Owner,
                    CompilerName = infoForm.CompilerName,
                    UnitWorkName = infoForm.UnitWorkName,
                    Scale = infoForm.Scale
                };

                exportService.Export(myMemoryQingdanBindingList.ToList(), info, sfd.FileName);
                AppLogger.Info($"导出成功: {sfd.FileName}");
                ErrorHandler.ShowBusiness($"导出成功：\n{sfd.FileName}", "导出完成");
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(ex, "导出失败");
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            MessageBox.Show(
                $"当前版本：{version}\n作者：Fancy\n数据目录：{AppConfig.DataDirectory}",
                "关于",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
