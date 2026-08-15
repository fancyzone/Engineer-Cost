using 施工定额.Entity;
using 施工定额.Helper;

namespace 施工定额.UI
{
    /// <summary>
    /// 费率设置：编辑后写入 %AppData%\施工定额\fee_settings.json。
    /// </summary>
    public class FeeSettingsForm : Form
    {
        private readonly ComboBox _cmbOverheadBase = new();
        private readonly NumericUpDown _numOverhead = new();
        private readonly NumericUpDown _numProfit = new();
        private readonly NumericUpDown _numStatutory = new();
        private readonly NumericUpDown _numVat = new();
        private readonly CheckBox _chkIncludeStatutory = new();

        public FeeRateSettings ResultRates { get; private set; } = new();

        public FeeSettingsForm(FeeRateSettings current)
        {
            Text = "费率设置";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 280);
            ShowInTaskbar = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            void StylePercent(NumericUpDown n)
            {
                n.DecimalPlaces = 2;
                n.Minimum = 0;
                n.Maximum = 100;
                n.Increment = 0.1m;
                n.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            }

            StylePercent(_numOverhead);
            StylePercent(_numProfit);
            StylePercent(_numStatutory);
            StylePercent(_numVat);

            _cmbOverheadBase.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbOverheadBase.Items.AddRange(new object[] { "DirectCost", "Labor" });
            _cmbOverheadBase.Anchor = AnchorStyles.Left | AnchorStyles.Right;

            _chkIncludeStatutory.Text = "综合合价包含规费";
            _chkIncludeStatutory.AutoSize = true;

            _cmbOverheadBase.SelectedItem =
                string.Equals(current.OverheadBase, "Labor", StringComparison.OrdinalIgnoreCase)
                    ? "Labor" : "DirectCost";
            _numOverhead.Value = ClampPercent(current.OverheadRate * 100);
            _numProfit.Value = ClampPercent(current.ProfitRate * 100);
            _numStatutory.Value = ClampPercent(current.StatutoryFeeRate * 100);
            _numVat.Value = ClampPercent(current.VatRate * 100);
            _chkIncludeStatutory.Checked = current.IncludeStatutoryInUnitPrice;

            void AddRow(int row, string label, Control c)
            {
                layout.Controls.Add(new Label
                {
                    Text = label,
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Margin = new Padding(0, 8, 0, 0)
                }, 0, row);
                layout.Controls.Add(c, 1, row);
            }

            AddRow(0, "管理费基数", _cmbOverheadBase);
            AddRow(1, "管理费率 (%)", _numOverhead);
            AddRow(2, "利润率 (%)", _numProfit);
            AddRow(3, "规费率 (%)", _numStatutory);
            AddRow(4, "增值税率 (%)", _numVat);
            layout.Controls.Add(_chkIncludeStatutory, 1, 5);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            var btnOk = new Button { Text = "保存", DialogResult = DialogResult.OK, AutoSize = true };
            var btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, AutoSize = true };
            buttons.Controls.Add(btnOk);
            buttons.Controls.Add(btnCancel);
            layout.Controls.Add(buttons, 0, 7);
            layout.SetColumnSpan(buttons, 2);

            Controls.Add(layout);
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            btnOk.Click += (_, _) =>
            {
                ResultRates = new FeeRateSettings
                {
                    OverheadBase = _cmbOverheadBase.SelectedItem?.ToString() ?? "DirectCost",
                    OverheadRate = _numOverhead.Value / 100m,
                    ProfitRate = _numProfit.Value / 100m,
                    StatutoryFeeRate = _numStatutory.Value / 100m,
                    VatRate = _numVat.Value / 100m,
                    IncludeStatutoryInUnitPrice = _chkIncludeStatutory.Checked
                };
            };
        }

        private static decimal ClampPercent(decimal value)
        {
            if (value < 0) return 0;
            if (value > 100) return 100;
            return Math.Round(value, 2);
        }
    }
}
