namespace 施工定额.UI
{
    /// <summary>
    /// 导出前填写项目基本信息。
    /// </summary>
    public class ExportProjectInfoForm : Form
    {
        private readonly TextBox _txtProjectName = new() { Width = 280 };
        private readonly TextBox _txtOwner = new() { Width = 280 };
        private readonly TextBox _txtCompiler = new() { Width = 280 };
        private readonly TextBox _txtUnitWork = new() { Width = 280 };
        private readonly TextBox _txtScale = new() { Width = 280 };

        public string ProjectName => _txtProjectName.Text.Trim();
        public string Owner => _txtOwner.Text.Trim();
        public string CompilerName => _txtCompiler.Text.Trim();
        public string UnitWorkName => _txtUnitWork.Text.Trim();
        public string Scale => _txtScale.Text.Trim();

        public ExportProjectInfoForm()
        {
            Text = "导出项目信息";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 260);
            Font = new Font("Microsoft YaHei UI", 9F);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            void AddRow(int row, string label, Control control)
            {
                layout.Controls.Add(new Label
                {
                    Text = label,
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Margin = new Padding(0, 8, 0, 0)
                }, 0, row);
                control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                layout.Controls.Add(control, 1, row);
            }

            _txtProjectName.Text = "示例项目";
            _txtOwner.Text = "建设单位名称";
            _txtCompiler.Text = "编制单位名称";
            _txtUnitWork.Text = "示例单位工程";
            _txtScale.Text = "20000 m2";

            AddRow(0, "项目名称", _txtProjectName);
            AddRow(1, "建设单位", _txtOwner);
            AddRow(2, "编制单位", _txtCompiler);
            AddRow(3, "单位工程", _txtUnitWork);
            AddRow(4, "建设规模", _txtScale);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            var btnOk = new Button { Text = "确定", DialogResult = DialogResult.OK, AutoSize = true };
            var btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, AutoSize = true };
            buttons.Controls.Add(btnOk);
            buttons.Controls.Add(btnCancel);
            layout.Controls.Add(buttons, 0, 5);
            layout.SetColumnSpan(buttons, 2);

            Controls.Add(layout);
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }
    }
}
