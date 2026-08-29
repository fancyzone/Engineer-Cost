namespace 施工定额
{
    public class ImageViewerForm : Form
    {
        private readonly List<string> _files;
        private int _index = 0;

        private PictureBox _pictureBox = null!;
        private Label _lblInfo = null!;
        private Button _btnPrev = null!, _btnNext = null!;

        public ImageViewerForm(string title, List<string> files)
        {
            _files = files;
            Text = $"图片查看 - {title}";
            Width = 1200;
            Height = 800;
            StartPosition = FormStartPosition.CenterParent;
            BuildUI();
            UI.UiTheme.ApplyTo(this);
            LoadImage();
        }

        private void BuildUI()
        {
            _pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };

            _lblInfo = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = UI.UiTheme.Font
            };

            var panel = new Panel { Dock = DockStyle.Bottom, Height = 44 };

            _btnPrev = new Button { Text = "◀ 上一张", Width = 100, Height = 34, Left = 10, Top = 5 };
            _btnNext = new Button { Text = "下一张 ▶", Width = 100, Height = 34, Left = 120, Top = 5 };

            _btnPrev.Click += (_, _) => Navigate(-1);
            _btnNext.Click += (_, _) => Navigate(1);

            panel.Controls.AddRange(new Control[] { _btnPrev, _btnNext });

            Controls.Add(_pictureBox);
            Controls.Add(_lblInfo);
            Controls.Add(panel);
            KeyPreview = true;
            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Left) Navigate(-1);
                if (e.KeyCode == Keys.Right) Navigate(1);
            };
        }

        private void Navigate(int delta)
        {
            _index = (_index + delta + _files.Count) % _files.Count;
            LoadImage();
        }

        private void LoadImage()
        {
            _pictureBox.Image?.Dispose();
            _pictureBox.Image = null;

            try
            {
                var ms = new MemoryStream(File.ReadAllBytes(_files[_index]));
                _pictureBox.Image = Image.FromStream(ms);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"图片加载失败：{ex.Message}");
            }

            _lblInfo.Text = $"{_index + 1} / {_files.Count}   {Path.GetFileName(_files[_index])}";
            _btnPrev.Enabled = _files.Count > 1;
            _btnNext.Enabled = _files.Count > 1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _pictureBox.Image?.Dispose();
            base.Dispose(disposing);
        }
    }
}
