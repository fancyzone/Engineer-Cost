using 施工定额.Helper;

namespace 施工定额.UI
{
    /// <summary>
    /// 统一管理 DataGridView 右键菜单。
    /// 删除等业务走 Presenter，不再直接依赖 Repository。
    /// </summary>
    public class ContextMenuBuilder
    {
        private readonly QingdanPresenter _presenter;
        private readonly SelectionState _selection;
        private readonly Action _reloadAll;

        public ContextMenuBuilder(
            QingdanPresenter presenter,
            SelectionState selection,
            Action reloadAll)
        {
            _presenter = presenter;
            _selection = selection;
            _reloadAll = reloadAll;
        }

        public ContextMenuStrip BuildQingdanMenu(DataGridView dgv)
        {
            var menu = new ContextMenuStrip();

            var deleteItem = new ToolStripMenuItem("删除清单");
            deleteItem.Click += (_, _) => DeleteQingdan(dgv);
            menu.Items.Add(deleteItem);

            var newItem = new ToolStripMenuItem("新建清单");
            newItem.Click += (_, _) => CreateNewQingdan();
            menu.Items.Add(newItem);

            return menu;
        }

        public ContextMenuStrip BuildDingeMenu(DataGridView dgv)
        {
            return new ContextMenuStrip();
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
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(ex, "删除失败");
            }
        }

        private void CreateNewQingdan()
        {
            var f2 = new Form2("");
            f2.DataImported += () => _reloadAll();
            f2.Show();
        }
    }
}
