using System.ComponentModel;
using System.Diagnostics;
using 施工定额.Entity;
using 施工定额.Export;
using 施工定额.Helper;
using 施工定额.Service;
using 施工定额.UI;

namespace 施工定额
{
    public partial class Form1 : Form
    {
        /// <summary>全量清单（分部分项 + 措施），Presenter / 汇总使用。</summary>
        private readonly BindingList<Qingdan> _allQingdan = new BindingList<Qingdan>();

        /// <summary>分部分项视图（与 _allQingdan 中对象同一引用）。</summary>
        private readonly BindingList<Qingdan> myMemoryQingdanBindingList = new BindingList<Qingdan>();

        /// <summary>措施项目视图（单价 + 总价）。</summary>
        private readonly BindingList<Qingdan> _measureQingdanList = new BindingList<Qingdan>();

        private readonly BindingList<Dinge> _dingeBindingList = new BindingList<Dinge>();
        private readonly BindingList<Dinge> _measureDingeBindingList = new BindingList<Dinge>();
        private readonly BindingList<Xiaohaoliang> _xhlBindingList = new BindingList<Xiaohaoliang>();

        private readonly IQingdanRepository _repo;
        private ICostCalculationService _calcService;
        private readonly SelectionState _selection;
        private readonly QingdanPresenter _qingdanPresenter;
        private readonly SummaryPresenter _summaryPresenter;
        private readonly ContextMenuBuilder _menuBuilder;

        private TabPage? tabPage措施;
        private SplitContainer? measureSplit;
        private DataGridView? dataGridView_measure;
        private DataGridView? DataGridView_measure_dinge;

        private bool _suppressGridEvents;

        public void ReloadAndRecalculateEverything()
        {
            _qingdanPresenter.ReloadAll();
            RebuildCategoryViews();

            var stillExists = _allQingdan.Any(
                q => q.清单编码 == _selection.SelectedQingdanCode);
            if (!stillExists)
                _selection.SelectQingdan("");

            UpdateDisplay(DisplayType.Qingdan);
            UpdateDisplay(DisplayType.Dinge);
            UpdateDisplay(DisplayType.Xiaohaoliang);
        }

        /// <summary>按项目类别拆分到分部分项 / 措施两个网格视图。</summary>
        private void RebuildCategoryViews()
        {
            myMemoryQingdanBindingList.RaiseListChangedEvents = false;
            _measureQingdanList.RaiseListChangedEvents = false;
            try
            {
                myMemoryQingdanBindingList.Clear();
                _measureQingdanList.Clear();
                foreach (var qd in _allQingdan)
                {
                    if (QingdanCategory.IsMeasure(qd.项目类别))
                        _measureQingdanList.Add(qd);
                    else
                        myMemoryQingdanBindingList.Add(qd);
                }
            }
            finally
            {
                myMemoryQingdanBindingList.RaiseListChangedEvents = true;
                _measureQingdanList.RaiseListChangedEvents = true;
                myMemoryQingdanBindingList.ResetBindings();
                _measureQingdanList.ResetBindings();
            }
        }

        public Form1()
            : this(
                AppComposition.CreateQingdanRepository(),
                AppComposition.CreateCostCalculationService())
        {
        }

        public Form1(IQingdanRepository repo, ICostCalculationService calcService)
        {
            InitializeComponent();
            ApplyResponsiveLayout();
            EnsureMeasureTab();
            ApplyToolbarIcons();
            Text = "施工定额";

            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _calcService = calcService ?? throw new ArgumentNullException(nameof(calcService));
            _selection = new SelectionState();

            _qingdanPresenter = new QingdanPresenter(
                _repo, _calcService, _allQingdan, UpdateDisplay);
            _summaryPresenter = new SummaryPresenter(_allQingdan, _calcService);

            _menuBuilder = new ContextMenuBuilder(
                _qingdanPresenter,
                _selection,
                ReloadAndRecalculateEverything);
            dataGridView1.ContextMenuStrip = _menuBuilder.BuildQingdanMenu(dataGridView1);

            _selection.QingdanSelectionChanged += OnQingdanSelectionChanged;
            _selection.DingeSelectionChanged += OnDingeSelectionChanged;

            EnsureSettingsMenu();
            EnsureHelpMenu();
            WirePlaceholderMenus();
        }

        /// <summary>在主 Tab 中增加「措施项目」，布局模仿分部分项。</summary>
        private void EnsureMeasureTab()
        {
            if (tabControl1.TabPages.Cast<TabPage>().Any(t => t.Name == "tabPage措施"))
                return;

            tabPage措施 = new TabPage
            {
                Name = "tabPage措施",
                Text = "措施项目",
                UseVisualStyleBackColor = true,
                Padding = new Padding(3)
            };

            measureSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 5,
                Panel1MinSize = 0,
                Panel2MinSize = 0
            };

            dataGridView_measure = new DataGridView
            {
                Dock = DockStyle.Fill,
                Name = "dataGridView_measure",
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };
            dataGridView_measure.CellClick += dataGridView_measure_CellClick;
            dataGridView_measure.CellDoubleClick += dataGridView_measure_CellDoubleClick;
            dataGridView_measure.CellEndEdit += dataGridView_measure_CellEndEdit;

            DataGridView_measure_dinge = new DataGridView
            {
                Dock = DockStyle.Fill,
                Name = "DataGridView_measure_dinge",
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };
            DataGridView_measure_dinge.CellClick += DataGridView_measure_dinge_CellClick;
            DataGridView_measure_dinge.CellValueChanged += DataGridView_measure_dinge_CellValueChanged;

            measureSplit.Panel1.Controls.Add(dataGridView_measure);
            measureSplit.Panel2.Controls.Add(DataGridView_measure_dinge);
            tabPage措施.Controls.Add(measureSplit);

            // 插在「分部分项」之后
            var insertAt = 1;
            for (int i = 0; i < tabControl1.TabPages.Count; i++)
            {
                if (tabControl1.TabPages[i].Name == "tabPage1")
                {
                    insertAt = i + 1;
                    break;
                }
            }
            tabControl1.TabPages.Insert(insertAt, tabPage措施);

            Shown += (_, _) =>
            {
                if (measureSplit != null && measureSplit.Height > 80)
                {
                    try
                    {
                        measureSplit.SplitterDistance = (int)(measureSplit.Height * 0.55);
                    }
                    catch { /* 尺寸未就绪时忽略 */ }
                }
            };
        }

        private void EnsureSettingsMenu()
        {
            foreach (ToolStripItem item in menuStrip1.Items)
            {
                if (item.Text == "设置")
                    return;
            }

            var settingsMenu = new ToolStripMenuItem("设置");
            var feeItem = new ToolStripMenuItem("费率设置(&F)...");
            feeItem.Click += (_, _) => OpenFeeSettings();
            settingsMenu.DropDownItems.Add(feeItem);
            menuStrip1.Items.Add(settingsMenu);
        }

        private void EnsureHelpMenu()
        {
            foreach (ToolStripItem item in menuStrip1.Items)
            {
                if (item.Text is "帮助" or "帮助(&H)")
                    return;
            }

            var help = new ToolStripMenuItem("帮助(&H)");

            var about = new ToolStripMenuItem("关于(&A)...");
            about.Click += (_, _) => ShowAbout();

            var openLog = new ToolStripMenuItem("打开日志目录");
            openLog.Click += (_, _) => OpenFolder(AppLogger.LogDirectory);

            var openData = new ToolStripMenuItem("打开数据目录");
            openData.Click += (_, _) => OpenFolder(AppConfig.DataDirectory);

            var restore = new ToolStripMenuItem("从备份恢复用户库...");
            restore.Click += (_, _) => RestoreUserDbFromBackup();

            help.DropDownItems.Add(about);
            help.DropDownItems.Add(new ToolStripSeparator());
            help.DropDownItems.Add(openLog);
            help.DropDownItems.Add(openData);
            help.DropDownItems.Add(restore);
            menuStrip1.Items.Add(help);
        }

        private static void OpenFolder(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(ex, "打开目录失败");
            }
        }

        private void ShowAbout()
        {
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            ErrorHandler.ShowBusiness(
                $"施工定额（Engineer-Cost）\n\n" +
                $"版本：{ver}\n" +
                $"用户数据：{AppConfig.DataDirectory}\n" +
                $"系统定额库：{AppConfig.SystemDbFilePath}\n\n" +
                "个人学习项目。",
                "关于");
        }

        private void RestoreUserDbFromBackup()
        {
            var backups = UserDbBackup.ListBackups();
            if (backups.Count == 0)
            {
                ErrorHandler.ShowBusiness(
                    $"未找到备份文件。\n备份目录：{UserDbBackup.BackupDirectory}",
                    "恢复备份");
                return;
            }

            using var ofd = new OpenFileDialog
            {
                Title = "选择用户库备份",
                InitialDirectory = UserDbBackup.BackupDirectory,
                Filter = "SQLite 数据库 (*.db)|*.db|所有文件 (*.*)|*.*",
                CheckFileExists = true
            };

            if (ofd.ShowDialog(this) != DialogResult.OK)
                return;

            var confirm = MessageBox.Show(
                this,
                "恢复备份将覆盖当前用户库。\n" +
                "恢复前会自动再备份一份当前库。\n\n" +
                "建议关闭其他可能占用数据库的窗口后继续。\n\n是否继续？",
                "确认恢复",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                UserDbBackup.Restore(ofd.FileName, AppConfig.UserDbFilePath);
                ReloadAndRecalculateEverything();
                ErrorHandler.ShowBusiness("用户库已从备份恢复，并已重新加载数据。", "恢复备份");
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(ex, "恢复备份失败");
            }
        }

        private void WirePlaceholderMenus()
        {
            打开ToolStripMenuItem.Click += (_, _) =>
                ErrorHandler.ShowBusiness("「打开工程」尚未实现，当前工程数据保存在用户库中。", "提示");
            保存ToolStripMenuItem.Click += (_, _) =>
                ErrorHandler.ShowBusiness("数据已在编辑时自动保存到用户库，无需手动保存。", "提示");
        }

        private void OpenFeeSettings()
        {
            using var form = new FeeSettingsForm(AppConfig.FeeRates);
            if (form.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                AppConfig.SaveUserFeeRates(form.ResultRates);
                _calcService = AppComposition.CreateCostCalculationService(form.ResultRates);
                _qingdanPresenter.ReplaceCalcService(_calcService);
                _summaryPresenter.ReplaceCalcService(_calcService);
                ErrorHandler.ShowBusiness("费率已保存，并已按新费率重算全部清单。", "费率设置");
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(ex, "保存费率失败");
            }
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

            if (dataGridView_measure != null)
            {
                GridManager.BindOnce(dataGridView_measure, _measureQingdanList, GridColumns.MeasureQingdan);
                dataGridView_measure.ContextMenuStrip = _menuBuilder.BuildQingdanMenu(dataGridView_measure, QingdanCategory.措施项目);
            }

            if (DataGridView_measure_dinge != null)
                GridManager.BindOnce(DataGridView_measure_dinge, _measureDingeBindingList, GridColumns.Dinge);
        }

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
                            var currentQd = _allQingdan
                                .FirstOrDefault(q => q.清单编码 == _selection.SelectedQingdanCode);
                            var list = currentQd?.定额列表 ?? Enumerable.Empty<Dinge>();

                            if (currentQd != null && QingdanCategory.IsMeasure(currentQd.项目类别))
                            {
                                _measureDingeBindingList.ReplaceAll(list);
                                _dingeBindingList.ReplaceAll(Enumerable.Empty<Dinge>());
                            }
                            else
                            {
                                _dingeBindingList.ReplaceAll(list);
                                _measureDingeBindingList.ReplaceAll(Enumerable.Empty<Dinge>());
                            }
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
                            var currentQd = _allQingdan
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

        private void dataGridView_measure_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView_measure == null || e.RowIndex < 0) return;
            string code = dataGridView_measure.Rows[e.RowIndex].Cells["清单编码"].Value?.ToString() ?? "";
            _selection.SelectQingdan(code);
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            OpenImportFormFromGrid(dataGridView1, e.RowIndex);
        }

        private void dataGridView_measure_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView_measure == null) return;
            OpenImportFormFromGrid(dataGridView_measure, e.RowIndex);
        }

        private void OpenImportFormFromGrid(DataGridView grid, int rowIndex)
        {
            if (rowIndex < 0) return;

            string qingdanCode = grid.Rows[rowIndex].Cells["清单编码"].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(qingdanCode))
            {
                ErrorHandler.ShowBusiness("当前选中的清单编码为空，无法打开定额库。");
                return;
            }

            Form2 f2 = AppComposition.CreateImportForm(qingdanCode);

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
            if (string.IsNullOrEmpty(code))
                return;

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
            if (tabControl1.SelectedIndex < 0) return;
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

        private void HandleDingeCellValueChanged(DataGridView grid, BindingList<Dinge> binding, DataGridViewCellEventArgs e)
        {
            if (_suppressGridEvents || e.RowIndex < 0) return;

            grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            var currentQd = _allQingdan
                .FirstOrDefault(q => q.清单编码 == _selection.SelectedQingdanCode);
            if (currentQd == null) return;

            _suppressGridEvents = true;
            try
            {
                var colName = grid.Columns[e.ColumnIndex].Name;
                if (colName == "换算系数")
                {
                    var dg = binding.ElementAtOrDefault(e.RowIndex);
                    if (dg != null)
                        _qingdanPresenter.OnDingeConversionFactorChanged(currentQd, dg);
                    else
                        _qingdanPresenter.OnDingeChanged(currentQd);
                }
                else if (colName == "定额名称")
                {
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

        private void DataGridView_dinge_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            HandleDingeCellValueChanged(DataGridView_dinge, _dingeBindingList, e);
        }

        private void DataGridView_measure_dinge_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (DataGridView_measure_dinge == null) return;
            HandleDingeCellValueChanged(DataGridView_measure_dinge, _measureDingeBindingList, e);
        }

        private void DataGridView_dinge_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;
            string 定额编码 = DataGridView_dinge.Rows[e.RowIndex].Cells["定额编码"].Value?.ToString() ?? "";
            string ID号 = DataGridView_dinge.Rows[e.RowIndex].Cells["ID号"].Value?.ToString() ?? "";
            _selection.SelectDinge(定额编码, ID号);
        }

        private void DataGridView_measure_dinge_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (DataGridView_measure_dinge == null || e.RowIndex < 0) return;
            string 定额编码 = DataGridView_measure_dinge.Rows[e.RowIndex].Cells["定额编码"].Value?.ToString() ?? "";
            string ID号 = DataGridView_measure_dinge.Rows[e.RowIndex].Cells["ID号"].Value?.ToString() ?? "";
            _selection.SelectDinge(定额编码, ID号);
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            HandleQingdanCellEndEdit(myMemoryQingdanBindingList, e.RowIndex, dataGridView1.Columns[e.ColumnIndex].Name);
        }

        private void dataGridView_measure_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView_measure == null || e.RowIndex < 0) return;
            HandleQingdanCellEndEdit(_measureQingdanList, e.RowIndex, dataGridView_measure.Columns[e.ColumnIndex].Name);
        }

        private void HandleQingdanCellEndEdit(BindingList<Qingdan> list, int rowIndex, string colName)
        {
            var changedQd = list.ElementAtOrDefault(rowIndex);
            if (changedQd == null) return;

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

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            ExportCoordinator.ExportYdjc(this, _allQingdan, _calcService);
        }

        private async void toolStripButton1_Click(object sender, EventArgs e)
        {
            var btn = sender as ToolStripItem;
            try
            {
                if (btn != null)
                    btn.Enabled = false;
                Cursor = Cursors.WaitCursor;
                await UpdateCoordinator.CheckAllAsync(this, silentIfUpToDate: false);
            }
            finally
            {
                Cursor = Cursors.Default;
                if (btn != null)
                    btn.Enabled = true;
            }
        }

        private void ApplyToolbarIcons()
        {
            toolStripButton1.Image = CreateRefreshIcon();
            toolStripButton1.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            toolStripButton1.TextImageRelation = TextImageRelation.ImageAboveText;

            toolStripButton2.Image = CreateExportIcon();
            toolStripButton2.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            toolStripButton2.TextImageRelation = TextImageRelation.ImageAboveText;
            toolStripButton2.Text = "导出";
        }

        private static Bitmap CreateRefreshIcon()
        {
            const int size = 32;
            var bmp = new Bitmap(size, size);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var pen = new Pen(Color.FromArgb(0, 120, 215), 2.5f);
            var rect = new Rectangle(5, 5, size - 10, size - 10);
            g.DrawArc(pen, rect, 40, 280);

            using var brush = new SolidBrush(Color.FromArgb(0, 120, 215));
            PointF[] tip =
            {
                new PointF(size - 7, 8),
                new PointF(size - 14, 6),
                new PointF(size - 12, 14)
            };
            g.FillPolygon(brush, tip);
            return bmp;
        }

        private static Bitmap CreateExportIcon()
        {
            const int size = 32;
            var bmp = new Bitmap(size, size);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var pen = new Pen(Color.FromArgb(16, 124, 16), 2f);
            using var brush = new SolidBrush(Color.FromArgb(16, 124, 16));

            g.DrawRectangle(pen, 8, 4, 16, 22);
            g.DrawLine(pen, 18, 4, 18, 10);
            g.DrawLine(pen, 18, 10, 24, 10);
            g.DrawLine(pen, 18, 4, 24, 10);
            g.DrawLine(pen, 16, 14, 16, 22);
            PointF[] arrow =
            {
                new PointF(16, 24),
                new PointF(12, 20),
                new PointF(20, 20)
            };
            g.FillPolygon(brush, arrow);
            return bmp;
        }
    }
}
