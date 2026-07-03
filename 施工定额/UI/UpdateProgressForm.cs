namespace 施工定额.UI
{
    public class UpdateProgressForm : Form
    {
        private readonly ProgressBar _bar;
        private readonly Label _lbl;
        private readonly Button _btnCancel;
        private readonly CancellationTokenSource _cts;

        public CancellationToken Token => _cts.Token;
        public bool IsCancelledByUser { get; private set; }

        public UpdateProgressForm()
        {
            _cts = new CancellationTokenSource();

            Text = "正在更新定额库";
            Width = 420;
            Height = 260;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false; // 仍然不允许右上角关闭，改用下面的“取消”按钮统一处理

            _lbl = new Label
            {
                Text = "正在下载最新定额库数据...",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 12, 12, 0)
            };

            _bar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 24,
                Margin = new Padding(12),
                Minimum = 0,
                Maximum = 100,
                Style = ProgressBarStyle.Marquee
            };

            var barPanel = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(12, 0, 12, 0) };
            barPanel.Controls.Add(_bar);

            _btnCancel = new Button
            {
                Text = "取消",
                Width = 90,
                Height = 30,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Top = 90,
                Left = 420 - 90 - 24
            };
            _btnCancel.Click += (_, _) =>
            {
                IsCancelledByUser = true;
                _btnCancel.Enabled = false;
                _lbl.Text = "正在取消...";
                _cts.Cancel();
            };

            Controls.Add(_btnCancel);
            Controls.Add(barPanel);
            Controls.Add(_lbl);
        }

        public void SetProgress(int percent)
        {
            if (IsDisposed) return;
            _bar.Style = ProgressBarStyle.Blocks;
            _bar.Value = Math.Clamp(percent, 0, 100);
            _lbl.Text = $"正在下载最新定额库数据... {_bar.Value}%";
        }

        private void InitializeComponent()
        {

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _cts.Dispose();
            base.Dispose(disposing);
        }
    }
}