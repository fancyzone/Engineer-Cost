using System.ComponentModel;
using 施工定额.Entity;
using 施工定额.Helper;
using 施工定额.UI;

namespace 施工定额
{
    public partial class Form1
    {
        private TabPage? tabPage其他项目;
        private DataGridView? dataGridView_other;
        private readonly BindingList<OtherProjectItem> _otherItems = new BindingList<OtherProjectItem>();
        private OtherProjectRepository? _otherRepo;
        private bool _otherItemsInitialized;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            EnsureOtherItemsInitialized();
        }

        private void EnsureOtherItemsInitialized()
        {
            if (_otherItemsInitialized || IsDisposed)
                return;
            _otherItemsInitialized = true;

            try
            {
                _otherRepo = new OtherProjectRepository(AppConfig.UserDbConn);
                EnsureOtherItemsTab();
                BindOtherItemsGrid();
                LoadOtherItems();
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(ex, "初始化其他项目失败");
            }
        }

        private void EnsureOtherItemsTab()
        {
            if (tabControl1.TabPages.Cast<TabPage>().Any(t => t.Name == "tabPage其他项目"))
                return;

            tabPage其他项目 = new TabPage
            {
                Name = "tabPage其他项目",
                Text = "其他项目",
                UseVisualStyleBackColor = true,
                Padding = new Padding(3)
            };

            dataGridView_other = new DataGridView
            {
                Dock = DockStyle.Fill,
                Name = "dataGridView_other",
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };
            tabPage其他项目.Controls.Add(dataGridView_other);

            var insertAt = tabControl1.TabPages.Count;
            for (int i = 0; i < tabControl1.TabPages.Count; i++)
            {
                if (tabControl1.TabPages[i].Name == "tabPage措施")
                {
                    insertAt = i + 1;
                    break;
                }
            }
            tabControl1.TabPages.Insert(insertAt, tabPage其他项目);
        }

        private void BindOtherItemsGrid()
        {
            if (dataGridView_other == null) return;
            if (dataGridView_other.DataSource != null) return;

            GridManager.BindOnce(dataGridView_other, _otherItems, new List<ColumnConfig>
            {
                new() { FieldName = "名称", HeaderText = "名称", Width = 180, ReadOnly = true },
                new() { FieldName = "金额", HeaderText = "金额", Width = 120, ReadOnly = false,
                        Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight },
            });
            dataGridView_other.CellBeginEdit += dataGridView_other_CellBeginEdit;
            dataGridView_other.CellEndEdit += dataGridView_other_CellEndEdit;
        }

        private void LoadOtherItems()
        {
            if (_otherRepo == null) return;
            try
            {
                var list = _otherRepo.LoadOrSeed();
                _otherItems.RaiseListChangedEvents = false;
                try
                {
                    _otherItems.Clear();
                    foreach (var item in list)
                        _otherItems.Add(item);
                }
                finally
                {
                    _otherItems.RaiseListChangedEvents = true;
                    _otherItems.ResetBindings();
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(ex, "加载其他项目失败");
            }
        }

        private void dataGridView_other_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _otherItems.Count) return;
            if (!_otherItems[e.RowIndex].IsAmountEditable)
                e.Cancel = true;
        }

        private void dataGridView_other_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (_otherRepo == null || e.RowIndex < 0 || e.RowIndex >= _otherItems.Count) return;
            var item = _otherItems[e.RowIndex];
            if (!item.IsAmountEditable)
            {
                item.金额 = 0;
                return;
            }
            try
            {
                _otherRepo.SaveAmount(item.清单编码, item.金额);
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(ex, "保存其他项目失败");
            }
        }
    }
}
