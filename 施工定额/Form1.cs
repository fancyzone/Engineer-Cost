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
        private readonly BindingList<Qingdan> myMemoryQingdanBindingList = new BindingList<Qingdan>();
        private readonly BindingList<Dinge> _dingeBindingList = new BindingList<Dinge>();
        private readonly BindingList<Xiaohaoliang> _xhlBindingList = new BindingList<Xiaohaoliang>();

        private readonly IQingdanRepository _repo;
        private ICostCalculationService _calcService;
        private readonly SelectionState _selection;
        private readonly QingdanPresenter _qingdanPresenter;
        private readonly SummaryPresenter _summaryPresenter;
        private readonly ContextMenuBuilder _menuBuilder;

        /// <summary>主区域：左工程树 | 右内容</summary>
        private SplitContainer? _mainSplit;
        /// <summary>分部分项：上清单 | 下定额</summary>
        private SplitContainer? _qingdanSplit;
        /// <summary>右侧：上主 Tab | 下工料机</summary>
        private SplitContainer? _rightSplit;
        /// <summary>左侧工程结构树（占位，后续接入）</summary>
        private TreeView? treeProject;

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

        /// <summary>设计器 / 默认入口：使用组合根创建依赖。</summary>
        public Form1()
            : this(
                AppComposition.CreateQingdanRepository(),
                AppComposition.CreateCostCalculationService())
        {
        }

        /// <summary>可注入构造：便于测试与替换实现。</summary>
        public Form1(IQingdanRepository repo, ICostCalculationService calcService)
        {
            InitializeComponent();
            ApplyResponsiveLayout();
            ApplyToolbarIcons();
            Text = "施工定额";

            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _calcService = calcService ?? throw new ArgumentNullException(nameof(calcService));
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

            EnsureSettingsMenu();
            EnsureHelpMenu();
            WirePlaceholderMenus();
        }

        /// <summary>
        /// 布局结构：
        /// ┌─────────┬──────────────────────┐
        /// │ 工程树  │  清单 (可拖分隔条)     │
        /// │ (预留)  │  定额                  │
        /// │         ├──────────────────────┤
        /// │         │  工料机                │
        /// └─────────┴──────────────────────┘
        /// 使用 SplitContainer，避免简单 Dock.Fill 盖住定额表。
        /// </summary>
        private void ApplyResponsiveLayout()
        {
            menuStrip1.Dock = DockStyle.Top;
            toolStrip1.Dock = DockStyle.Top;

            // 从窗体卸下两个 Tab，改由分割容器托管
            Controls.Remove(tabControl1);
            Controls.Remove(tabControl2);

            _mainSplit = new SplitContainer
            {
                Name = "mainSplit",
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                SplitterWidth = 5,
                Panel1MinSize = 120,
                Panel2MinSize = 400
            };

            // 左侧：工程结构树占位（后续可绑清单/分部节点）
            treeProject = new TreeView
            {
                Name = "treeProject",
                Dock = DockStyle.Fill,
                HideSelection = false,
                ShowLines = true,
                ShowPlusMinus = true,
                Font = Font
            };
            treeProject.Nodes.Add(new TreeNode("工程结构（待接入）")
            {
                ForeColor = SystemColors.GrayText
            });
            _mainSplit.Panel1.Controls.Add(treeProject);

            // 右侧：上主 Tab（分部分项等） | 下工料机
            _rightSplit = new SplitContainer
            {
                Name = "rightSplit",
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BorderStyle = BorderStyle.FixedSingle,
                SplitterWidth = 5,
                Panel1MinSize = 180,
                Panel2MinSize = 120
            };

            tabControl1.Dock = DockStyle.Fill;
            tabControl2.Dock = DockStyle.Fill;
            _rightSplit.Panel1.Controls.Add(tabControl1);
            _rightSplit.Panel2.Controls.Add(tabControl2);
            _mainSplit.Panel2.Controls.Add(_rightSplit);

            // Fill 控件先加入，再保证菜单/工具栏在顶层 Dock.Top
            Controls.Add(_mainSplit);
            Controls.SetChildIndex(_mainSplit, 0);
            if (Controls.Contains(toolStrip1))
                Controls.SetChildIndex(toolStrip1, 0);
            if (Controls.Contains(menuStrip1))
                Controls.SetChildIndex(menuStrip1, 0);

            // 初始分隔比例（Handle 创建后再设，避免 DPI 下异常）
            Shown += (_, _) =>
            {
                try
                {
                    if (_mainSplit != null && _mainSplit.Width > 0)
                        _mainSplit.SplitterDistance = Math.Clamp(200, _mainSplit.Panel1MinSize,
                            Math.Max(_mainSplit.Panel1MinSize, _mainSplit.Width - _mainSplit.Panel2MinSize - _mainSplit.SplitterWidth));

                    if (_rightSplit != null && _rightSplit.Height > 0)
                    {
                        int prefer = (int)(_rightSplit.Height * 0.62);
                        _rightSplit.SplitterDistance = Math.Clamp(prefer, _rightSplit.Panel1MinSize,
                            Math.Max(_rightSplit.Panel1MinSize, _rightSplit.Height - _rightSplit.Panel2MinSize - _rightSplit.SplitterWidth));
                    }

                    if (_qingdanSplit != null && _qingdanSplit.Height > 0)
                    {
                        int prefer = (int)(_qingdanSplit.Height * 0.55);
                        _qingdanSplit.SplitterDistance = Math.Clamp(prefer, _qingdanSplit.Panel1MinSize,
                            Math.Max(_qingdanSplit.Panel1MinSize, _qingdanSplit.Height - _qingdanSplit.Panel2MinSize - _qingdanSplit.SplitterWidth));
                    }
                }
                catch
                {
                    // 忽略极端尺寸下的分隔条设置失败
                }
            };

            // 分部分项：清单上、定额下（可拖动）
            tabPage1.Controls.Remove(dataGridView1);
            tabPage1.Controls.Remove(DataGridView_dinge);

            _qingdanSplit = new SplitContainer
            {
                Name = "qingdanSplit",
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BorderStyle = BorderStyle.FixedSingle,
                SplitterWidth = 5,
                Panel1MinSize = 80,
                Panel2MinSize = 80
            };
            dataGridView1.Dock = DockStyle.Fill;
            DataGridView_dinge.Dock = DockStyle.Fill;
            _qingdanSplit.Panel1.Controls.Add(dataGridView1);
            _qingdanSplit.Panel2.Controls.Add(DataGridView_dinge);
            tabPage1.Controls.Add(_qingdanSplit);

            // 工料机 / 汇总页
            dataGridView2.Dock = DockStyle.Fill;
            dataGridView3.Dock = DockStyle.Fill;
            dataGridView4.Dock = DockStyle.Fill;

            // 人材机汇总：树左表右
            treeView1.Dock = DockStyle.Left;
            treeView1.Width = 160;
            if (tabRenCaiJi.Controls.Contains(treeView1) && tabRenCaiJi.Controls.Contains(dataGridView3))
            {
                tabRenCaiJi.Controls.SetChildIndex(treeView1, 0);
                tabRenCaiJi.Controls.SetChildIndex(dataGridView3, 1);
            }
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

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            ExportCoordinator.ExportYdjc(this, myMemoryQingdanBindingList, _calcService);
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

        /// <summary>为工具栏按钮绘制图标（检查更新 + 导出）。</summary>
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
