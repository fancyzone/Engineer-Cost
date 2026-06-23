using Dapper;
using System.ComponentModel;
using System.Data.SQLite;
using 施工定额.Entity;

namespace 施工定额.UI
{
    /// <summary>
    /// 统一管理所有 DataGridView 的右键菜单
    /// 每个 Build 方法负责一个表格的菜单，Form1 只调用 Build，不关心内部逻辑
    /// </summary>
    public class ContextMenuBuilder
    {
        private readonly QingdanRepository _repo;
        private readonly Func<BindingList<Qingdan>> _getQingdanList;
        private readonly Action<Form1.DisplayType> _updateDisplay;
        private readonly Action _reloadAll;  // ← 加这个字段

        public ContextMenuBuilder(
            QingdanRepository repo,
            Func<BindingList<Qingdan>> getQingdanList,
            Action<Form1.DisplayType> updateDisplay,
            Action reloadAll)
        {
            _repo = repo;
            _getQingdanList = getQingdanList;
            _updateDisplay = updateDisplay;
            _reloadAll = reloadAll;
        }

        /// <summary>
        /// 构建清单表格（dataGridView1）的右键菜单
        /// </summary>
        public ContextMenuStrip BuildQingdanMenu(DataGridView dgv)
        {
            var menu = new ContextMenuStrip();

            var deleteItem = new ToolStripMenuItem("删除清单");
            deleteItem.Click += (_, _) => DeleteQingdan(dgv);
            menu.Items.Add(deleteItem);

            // ContextMenuBuilder.cs - BuildQingdanMenu 里追加
            var newItem = new ToolStripMenuItem("新建清单");
            newItem.Click += (_, _) => CreateNewQingdan(dgv);
            menu.Items.Add(newItem);

            return menu;
        }

        /// <summary>
        /// 构建定额表格（DataGridView_dinge）的右键菜单
        /// </summary>
        public ContextMenuStrip BuildDingeMenu(DataGridView dgv)
        {
            var menu = new ContextMenuStrip();

            // 后续有需要再加
            // var deleteItem = new ToolStripMenuItem("删除定额");
            // deleteItem.Click += (_, _) => DeleteDinge(dgv);
            // menu.Items.Add(deleteItem);

            return menu;
        }

        // ── 具体操作 ────────────────────────────────────────────

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
                _repo.DeleteQingdan(code);

                var list = _getQingdanList();  // ← 每次现取
                var toRemove = list.FirstOrDefault(q => q.清单编码 == code);
                if (toRemove != null)
                    list.Remove(toRemove);

                if (SelectionState.Instance.SelectedQingdanCode == code)
                    SelectionState.Instance.SelectQingdan("");

                _updateDisplay(Form1.DisplayType.Qingdan);
                _updateDisplay(Form1.DisplayType.Dinge);
                _updateDisplay(Form1.DisplayType.Xiaohaoliang);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void CreateNewQingdan(DataGridView dgv)
        {
            var f2 = new Form2("");
            f2.DataImported += () => _reloadAll();
            f2.Show();
        }
    }
}