namespace 施工定额.UI
{
    /// <summary>
    /// 全程序统一 UI 字体（微软雅黑 UI 10.5pt），与 Form1 网格风格一致。
    /// </summary>
    public static class UiTheme
    {
        public static readonly Font Font = new Font("Microsoft YaHei UI", 10.5f);
        public static readonly Font HeaderFont = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold);

        /// <summary>
        /// 在 Application.Run 之前调用，影响后续创建的默认控件字体。
        /// MessageBox 仍可能使用系统字体（系统对话框限制）。
        /// </summary>
        public static void ApplyApplicationDefaults()
        {
            try
            {
                Application.SetDefaultFont(Font);
            }
            catch
            {
                // 旧运行时无此 API 时忽略
            }
        }

        /// <summary>递归设置窗体/控件树字体，含菜单、工具栏、状态栏、表格。</summary>
        public static void ApplyTo(Control root)
        {
            if (root == null) return;
            try { root.Font = Font; } catch { /* 部分控件只读 */ }

            ApplyToolStrip(root.ContextMenuStrip);
            if (root is ToolStrip ts)
                ApplyToolStrip(ts);

            if (root is DataGridView dgv)
                ApplyDataGridView(dgv);

            foreach (Control child in root.Controls)
                ApplyTo(child);

            if (root is Form form)
            {
                if (form.MainMenuStrip != null)
                    ApplyToolStrip(form.MainMenuStrip);
            }
        }

        public static void ApplyDataGridView(DataGridView dgv)
        {
            if (dgv == null) return;
            dgv.Font = Font;
            dgv.DefaultCellStyle.Font = Font;
            dgv.ColumnHeadersDefaultCellStyle.Font = HeaderFont;
            dgv.RowHeadersDefaultCellStyle.Font = Font;
            if (dgv.RowTemplate.Height < 28)
                dgv.RowTemplate.Height = 28;
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.DefaultCellStyle != null)
                    col.DefaultCellStyle.Font = Font;
            }
        }

        public static void ApplyToolStrip(ToolStrip? strip)
        {
            if (strip == null) return;
            strip.Font = Font;
            foreach (ToolStripItem item in strip.Items)
                ApplyToolStripItem(item);
        }

        private static void ApplyToolStripItem(ToolStripItem item)
        {
            item.Font = Font;
            if (item is ToolStripDropDownItem dd)
            {
                dd.DropDown.Font = Font;
                foreach (ToolStripItem sub in dd.DropDownItems)
                    ApplyToolStripItem(sub);
            }
        }
    }
}
