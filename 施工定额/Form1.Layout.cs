using 施工定额.Helper;

namespace 施工定额
{
    /// <summary>
    /// 布局已全部在 Form1.Designer 中定义（设计器与运行时一致）。
    /// 此处仅在首次显示时微调分隔条比例，避免极端 DPI/窗口尺寸下的不适。
    /// </summary>
    public partial class Form1
    {
        /// <summary>保留空方法名兼容构造函数调用；实际只挂接 Shown 微调。</summary>
        private void ApplyResponsiveLayout()
        {
            // 菜单 / 工具栏 Dock 已由 Designer 与 Controls 添加顺序保证：
            // Controls: mainSplit → toolStrip1 → menuStrip1
            MainMenuStrip = menuStrip1;
            Shown += OnFirstShownFineTuneSplitters;
        }

        private void OnFirstShownFineTuneSplitters(object? sender, EventArgs e)
        {
            Shown -= OnFirstShownFineTuneSplitters;
            BeginInvoke(new Action(FineTuneSplitters));
        }

        private void FineTuneSplitters()
        {
            try
            {
                // 仅在比例明显不合理时调整，不强制重建控件树
                if (mainSplit.Width > 400 && mainSplit.SplitterDistance < 80)
                    SafeSetSplit(mainSplit, preferred: 200, panel1Min: 120, panel2Min: 200);

                if (rightSplit.Height > 200)
                {
                    int prefer = (int)(rightSplit.Height * 0.62);
                    if (Math.Abs(rightSplit.SplitterDistance - prefer) > rightSplit.Height / 4)
                        SafeSetSplit(rightSplit, preferred: prefer, panel1Min: 150, panel2Min: 100);
                }

                if (qingdanSplit.Height > 120)
                {
                    int prefer = (int)(qingdanSplit.Height * 0.55);
                    if (Math.Abs(qingdanSplit.SplitterDistance - prefer) > qingdanSplit.Height / 4)
                        SafeSetSplit(qingdanSplit, preferred: prefer, panel1Min: 60, panel2Min: 60);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("微调分隔条失败", ex);
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
            split.SplitterDistance = Math.Clamp(preferred, minDist, maxDist);

            split.Panel1MinSize = p1;
            split.Panel2MinSize = p2;
        }
    }
}
