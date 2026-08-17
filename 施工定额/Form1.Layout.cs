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
        /// ┌─────────┬──────────────────────┐
        /// │ 工程树  │  清单 (可拖分隔条)     │
        /// │ (预留)  │  定额                  │
        /// │         ├──────────────────────┤
        /// │         │  工料机                │
        /// └─────────┴──────────────────────┘
        /// </summary>
        private void ApplyResponsiveLayout()
        {
            try
            {
                menuStrip1.Dock = DockStyle.Top;
                toolStrip1.Dock = DockStyle.Top;

                Controls.Remove(tabControl1);
                Controls.Remove(tabControl2);

                // 注意：构造时不要设置过大的 Panel1MinSize/Panel2MinSize。
                // 父控件尺寸尚未就绪时，WinForms 会抛 InvalidOperationException。
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

                tabControl1.Dock = DockStyle.Fill;
                tabControl2.Dock = DockStyle.Fill;
                _rightSplit.Panel1.Controls.Add(tabControl1);
                _rightSplit.Panel2.Controls.Add(tabControl2);
                _mainSplit.Panel2.Controls.Add(_rightSplit);

                Controls.Add(_mainSplit);
                // Dock.Fill 放在底层，Top 菜单/工具栏在上
                Controls.SetChildIndex(_mainSplit, 0);
                if (Controls.Contains(toolStrip1))
                    Controls.SetChildIndex(toolStrip1, 0);
                if (Controls.Contains(menuStrip1))
                    Controls.SetChildIndex(menuStrip1, 0);

                // 分部分项：清单上、定额下
                tabPage1.Controls.Remove(dataGridView1);
                tabPage1.Controls.Remove(DataGridView_dinge);

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
                dataGridView1.Dock = DockStyle.Fill;
                DataGridView_dinge.Dock = DockStyle.Fill;
                _qingdanSplit.Panel1.Controls.Add(dataGridView1);
                _qingdanSplit.Panel2.Controls.Add(DataGridView_dinge);
                tabPage1.Controls.Add(_qingdanSplit);

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

                // 尺寸就绪后再设分隔比例与最小尺寸
                Shown += OnFirstShownApplySplitters;
            }
            catch (Exception ex)
            {
                AppLogger.Error("ApplyResponsiveLayout 失败", ex);
                // 不 rethrow：保留设计器原始布局，至少能进主界面
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
                    SafeSetSplit(_rightSplit, preferred: prefer, panel1Min: 120, panel2Min: 100);
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

            // 先降 Min，再设 Distance，最后再抬 Min，避免 InvalidOperationException
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
