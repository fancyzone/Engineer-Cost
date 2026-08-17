using 施工定额.Helper;

namespace 施工定额
{
    /// <summary>
    /// Form1 布局相关（与业务代码分离，便于维护）。
    /// SplitContainer 的 MinSize / SplitterDistance 必须在控件有有效尺寸后再设，
    /// 否则会在构造阶段抛异常，导致 Ctrl+F5 时进程直接退出、看不到界面。
    /// </summary>
    public partial class Form1
    {
        /// <summary>
        /// 布局结构：
        /// 菜单栏
        /// 工具栏
        /// ┌─────────┬──────────────────────┐
        /// │ 工程树  │  [分部分项|汇总…] Tab  │
        /// │ (预留)  │  清单 / 定额           │
        /// │         ├──────────────────────┤
        /// │         │  工料机                │
        /// └─────────┴──────────────────────┘
        /// </summary>
        private void ApplyResponsiveLayout()
        {
            try
            {
                // —— 菜单在上、工具栏在下（Dock.Top 按 z-order 从高到低布局）——
                // 较高 z-order 的控件先 Dock，因此 menuStrip 必须在 toolStrip 之上。
                menuStrip1.Dock = DockStyle.Top;
                toolStrip1.Dock = DockStyle.Top;
                MainMenuStrip = menuStrip1;

                Controls.Remove(tabControl1);
                Controls.Remove(tabControl2);

                // 去掉设计器残留的 Anchor，避免与 Dock.Fill 冲突导致页签被裁切
                tabControl1.Anchor = AnchorStyles.None;
                tabControl2.Anchor = AnchorStyles.None;
                tabControl1.Dock = DockStyle.Fill;
                tabControl2.Dock = DockStyle.Fill;
                tabControl1.Visible = true;
                tabControl2.Visible = true;

                // 确保主 Tab 仍包含预期页（分部分项 / 人材机 / 费用汇总）
                EnsureMainTabs();

                _mainSplit = new SplitContainer
                {
                    Name = "mainSplit",
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Vertical,
                    BorderStyle = BorderStyle.FixedSingle,
                    SplitterWidth = 5,
                    Panel1MinSize = 0,
                    Panel2MinSize = 0
                };

                treeProject = new TreeView
                {
                    Name = "treeProject",
                    Dock = DockStyle.Fill,
                    HideSelection = false,
                    ShowLines = true,
                    ShowPlusMinus = true
                };
                treeProject.Nodes.Add(new TreeNode("工程结构（待接入）")
                {
                    ForeColor = SystemColors.GrayText
                });
                _mainSplit.Panel1.Controls.Add(treeProject);

                _rightSplit = new SplitContainer
                {
                    Name = "rightSplit",
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Horizontal,
                    BorderStyle = BorderStyle.FixedSingle,
                    SplitterWidth = 5,
                    Panel1MinSize = 0,
                    Panel2MinSize = 0
                };

                _rightSplit.Panel1.Controls.Add(tabControl1);
                _rightSplit.Panel2.Controls.Add(tabControl2);
                _mainSplit.Panel2.Controls.Add(_rightSplit);

                // 加入主分割区
                if (!Controls.Contains(_mainSplit))
                    Controls.Add(_mainSplit);

                // z-order 自下而上：内容 → 工具栏 → 菜单
                // Dock.Top 从高 z-order 开始占位 → 菜单在最顶，工具栏在其下
                Controls.SetChildIndex(_mainSplit, 0);
                if (Controls.Contains(toolStrip1))
                    Controls.SetChildIndex(toolStrip1, Controls.Count - 2);
                if (Controls.Contains(menuStrip1))
                    Controls.SetChildIndex(menuStrip1, Controls.Count - 1);

                // 再保险：按标准顺序重新挂一遍菜单/工具栏
                Controls.Remove(toolStrip1);
                Controls.Remove(menuStrip1);
                Controls.Add(toolStrip1);
                Controls.Add(menuStrip1);
                // 此时顺序（低→高 z）：… _mainSplit, toolStrip1, menuStrip1

                // 分部分项：清单上、定额下
                tabPage1.Controls.Remove(dataGridView1);
                tabPage1.Controls.Remove(DataGridView_dinge);

                dataGridView1.Anchor = AnchorStyles.None;
                DataGridView_dinge.Anchor = AnchorStyles.None;
                dataGridView1.Dock = DockStyle.Fill;
                DataGridView_dinge.Dock = DockStyle.Fill;

                _qingdanSplit = new SplitContainer
                {
                    Name = "qingdanSplit",
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Horizontal,
                    BorderStyle = BorderStyle.FixedSingle,
                    SplitterWidth = 5,
                    Panel1MinSize = 0,
                    Panel2MinSize = 0
                };
                _qingdanSplit.Panel1.Controls.Add(dataGridView1);
                _qingdanSplit.Panel2.Controls.Add(DataGridView_dinge);
                tabPage1.Controls.Clear();
                tabPage1.Controls.Add(_qingdanSplit);

                dataGridView2.Anchor = AnchorStyles.None;
                dataGridView3.Anchor = AnchorStyles.None;
                dataGridView4.Anchor = AnchorStyles.None;
                dataGridView2.Dock = DockStyle.Fill;
                dataGridView3.Dock = DockStyle.Fill;
                dataGridView4.Dock = DockStyle.Fill;

                treeView1.Dock = DockStyle.Left;
                treeView1.Width = 160;
                if (tabRenCaiJi.Controls.Contains(treeView1) && tabRenCaiJi.Controls.Contains(dataGridView3))
                {
                    tabRenCaiJi.Controls.SetChildIndex(treeView1, 0);
                    tabRenCaiJi.Controls.SetChildIndex(dataGridView3, 1);
                }

                Shown += OnFirstShownApplySplitters;
            }
            catch (Exception ex)
            {
                AppLogger.Error("ApplyResponsiveLayout 失败", ex);
            }
        }

        /// <summary>保证主 Tab 页齐全且文案正确。</summary>
        private void EnsureMainTabs()
        {
            tabControl1.TabPages.Clear();

            tabPage1.Text = "分部分项";
            tabRenCaiJi.Text = "人材机汇总";
            tabCostSummary.Text = "费用汇总";

            tabControl1.TabPages.Add(tabPage1);
            tabControl1.TabPages.Add(tabRenCaiJi);
            tabControl1.TabPages.Add(tabCostSummary);
            tabControl1.SelectedTab = tabPage1;

            // 工料机区
            if (!tabControl2.TabPages.Contains(tabPage3))
            {
                tabControl2.TabPages.Clear();
                tabPage3.Text = "工料机";
                tabControl2.TabPages.Add(tabPage3);
            }
            else
            {
                tabPage3.Text = "工料机";
            }
        }

        private void OnFirstShownApplySplitters(object? sender, EventArgs e)
        {
            Shown -= OnFirstShownApplySplitters;
            BeginInvoke(new Action(ApplySplitterDistancesSafely));
        }

        private void ApplySplitterDistancesSafely()
        {
            try
            {
                SafeSetSplit(_mainSplit, preferred: 200, panel1Min: 120, panel2Min: 200);
                if (_rightSplit != null && _rightSplit.Height > 50)
                {
                    int prefer = (int)(_rightSplit.Height * 0.62);
                    SafeSetSplit(_rightSplit, preferred: prefer, panel1Min: 150, panel2Min: 100);
                }

                if (_qingdanSplit != null && _qingdanSplit.Height > 50)
                {
                    int prefer = (int)(_qingdanSplit.Height * 0.55);
                    SafeSetSplit(_qingdanSplit, preferred: prefer, panel1Min: 60, panel2Min: 60);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("设置分隔条位置失败", ex);
            }
        }

        private static void SafeSetSplit(SplitContainer? split, int preferred, int panel1Min, int panel2Min)
        {
            if (split == null || split.IsDisposed)
                return;

            int span = split.Orientation == Orientation.Vertical ? split.Width : split.Height;
            if (span <= 0)
                return;

            int splitter = Math.Max(1, split.SplitterWidth);
            int maxMin = Math.Max(0, (span - splitter) / 3);
            int p1 = Math.Min(panel1Min, maxMin);
            int p2 = Math.Min(panel2Min, maxMin);

            split.Panel1MinSize = 0;
            split.Panel2MinSize = 0;

            int maxDist = Math.Max(0, span - splitter - p2);
            int minDist = Math.Min(p1, maxDist);
            int dist = Math.Clamp(preferred, minDist, maxDist);
            split.SplitterDistance = dist;

            split.Panel1MinSize = p1;
            split.Panel2MinSize = p2;
        }
    }
}
