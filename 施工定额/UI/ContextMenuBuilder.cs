using 施工定额.Entity;
using 施工定额.Helper;

namespace 施工定额.UI
{
    public class ContextMenuBuilder
    {
        private readonly QingdanPresenter _presenter;
        private readonly SelectionState _selection;
        private readonly Action _reloadAll;
        private readonly Func<string> _getUnitProjectCode;

        public ContextMenuBuilder(
            QingdanPresenter presenter,
            SelectionState selection,
            Action reloadAll,
            Func<string>? getUnitProjectCode = null)
        {
            _presenter = presenter;
            _selection = selection;
            _reloadAll = reloadAll;
            _getUnitProjectCode = getUnitProjectCode ?? (() => UnitProject.DefaultCode);
        }

        public ContextMenuStrip BuildQingdanMenu(DataGridView dgv,
            string qingdanCategory = QingdanCategory.分部分项)
        {
            var menu = new ContextMenuStrip { Font = UiTheme.Font };
            var category = QingdanCategory.Normalize(qingdanCategory);

            var deleteItem = new ToolStripMenuItem("删除清单");
            deleteItem.Click += (_, _) => DeleteQingdan(dgv);
            menu.Items.Add(deleteItem);

            var newItem = new ToolStripMenuItem("新建清单");
            newItem.Click += (_, _) =>
            {
                var cat = category;
                if (menu.SourceControl is DataGridView g && g.Tag is string tagCat)
                    cat = QingdanCategory.Normalize(tagCat);
                CreateNewQingdan(cat);
            };
            menu.Items.Add(newItem);
            UiTheme.ApplyToolStrip(menu);
            return menu;
        }

        public ContextMenuStrip BuildDingeMenu(DataGridView dgv)
        {
            return new ContextMenuStrip { Font = UiTheme.Font };
        }

        private void DeleteQingdan(DataGridView dgv)
        {
            if (dgv.CurrentRow == null) return;

            string code = dgv.CurrentRow.Cells["清单编码"].Value?.ToString() ?? "";
            string name = dgv.CurrentRow.Cells["清单名称"].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(code)) return;

            var confirm = MessageBox.Show(
                $"确定要删除清单「{name}」及其所有定额和消耗量吗？\n此操作不可撤销。",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _presenter.DeleteQingdan(code);

                if (_selection.SelectedQingdanCode == code)
                    _selection.SelectQingdan("");

                _reloadAll();
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(ex, "删除失败");
            }
        }

        private void CreateNewQingdan(string qingdanCategory)
        {
            var f2 = new Form2("", qingdanCategory, _getUnitProjectCode());
            f2.DataImported += () => _reloadAll();
            f2.Show();
        }
    }
}
