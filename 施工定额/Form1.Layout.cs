using 施工定额.Helper;

namespace 施工定额
{
    /// <summary>
    /// 布局结构在 Form1.Designer 中定义。
    /// SplitContainer 的 MinSize / SplitterDistance 必须在控件有最终尺寸后再设，
    /// 否则 InitializeComponent 会抛：SplitterDistance 必须在 Panel1MinSize 和 Width-Panel2MinSize 之间。
    /// </summary>
    public partial class Form1
    {
        private void ApplyResponsiveLayout()
        {
            MainMenuStrip = menuStrip1;
            Shown += OnFirstShownApplySplitters;
        }

        private void OnFirstShownApplySplitters(object? sender, EventArgs e)
        {
            Shown -= OnFirstShownApplySplitters;
            BeginInvoke(new Action(ApplySplittersAfterLayout));
        }

        private void ApplySplittersAfterLayout()
        {
            try
            {
                SafeSetSplit(mainSplit, preferred: 200, panel1Min: 120, panel2Min: 200);

                if (rightSplit.Height > 50)
                {
                    int prefer = Math.Max(150, (int)(rightSplit.Height * 0.62));
                    SafeSetSplit(rightSplit, preferred: prefer, panel1Min: 120, panel2Min: 80);
                }

                if (qingdanSplit.Height > 50)
                {
                    int prefer = Math.Max(80, (int)(qingdanSplit.Height * 0.55));
                    SafeSetSplit(qingdanSplit, preferred: prefer, panel1Min: 60, panel2Min: 60);
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
            if (span <= 10)
                return;

            int splitter = Math.Max(1, split.SplitterWidth);
            // 可用空间不够时压低 Min，避免异常
            int maxMin = Math.Max(0, (span - splitter) / 3);
            int p1 = Math.Min(panel1Min, maxMin);
            int p2 = Math.Min(panel2Min, maxMin);

            split.Panel1MinSize = 0;
            split.Panel2MinSize = 0;

            int maxDist = Math.Max(0, span - splitter - p2);
            int minDist = Math.Min(p1, maxDist);
            if (maxDist < minDist)
                return;

            split.SplitterDistance = Math.Clamp(preferred, minDist, maxDist);
            split.Panel1MinSize = p1;
            split.Panel2MinSize = p2;
        }
    }
}
