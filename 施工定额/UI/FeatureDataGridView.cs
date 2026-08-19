using System.ComponentModel;

namespace 施工定额.UI
{
    /// <summary>
    /// 支持项目特征等多行单元格：Alt+Enter 插入换行。
    /// DataGridView 会在更底层把 Enter 当成结束编辑，必须在 ProcessCmdKey 拦截。
    /// </summary>
    public class FeatureDataGridView : DataGridView
    {
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.Enter
                && (keyData & Keys.Alt) == Keys.Alt
                && IsCurrentCellInEditMode
                && EditingControl is TextBox tb)
            {
                tb.SelectedText = Environment.NewLine;
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.Enter
                && (keyData & Keys.Alt) == Keys.Alt
                && IsCurrentCellInEditMode
                && EditingControl is TextBox tb)
            {
                tb.SelectedText = Environment.NewLine;
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        protected override bool ProcessDataGridViewKey(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && e.Alt
                && IsCurrentCellInEditMode
                && EditingControl is TextBox tb)
            {
                tb.SelectedText = Environment.NewLine;
                return true;
            }
            return base.ProcessDataGridViewKey(e);
        }
    }
}
