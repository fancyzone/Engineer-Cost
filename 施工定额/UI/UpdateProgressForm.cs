namespace 施工定额.UI
{
    public class UpdateProgressForm : Form
    {
        private readonly ProgressBar _bar;
        private readonly Label _lbl;

        public UpdateProgressForm()
        {
            Text = "正在更新定额库";
            Width = 420;
            Height = 130;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;

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

            var panel = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(12, 0, 12, 0) };
            panel.Controls.Add(_bar);

            Controls.Add(panel);
            Controls.Add(_lbl);
        }

        public void SetProgress(int percent)
        {
            _bar.Style = ProgressBarStyle.Blocks;
            _bar.Value = Math.Clamp(percent, 0, 100);
            _lbl.Text = $"正在下载最新定额库数据... {_bar.Value}%";
        }
    }
}