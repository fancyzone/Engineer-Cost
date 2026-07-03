namespace 施工定额
{
    /// <summary>
    /// 一个不可见的“引导窗体”，唯一作用是让 Application.Run 尽早启动消息泵，
    /// 这样在它的 Shown 事件里再触发异步初始化逻辑时，
    /// 后续弹出的所有子窗口（比如更新进度条）才能正常响应用户输入。
    /// 初始化完成后自动关闭自身，交回控制权继续走 Application.Run(new Form1())。
    /// </summary>
    public class BootstrapForm : Form
    {
        public Func<Task>? RunBootstrap { get; set; }

        public BootstrapForm()
        {
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            FormBorderStyle = FormBorderStyle.None;
            Size = new Size(1, 1);
            Load += async (_, _) =>
            {
                if (RunBootstrap != null)
                    await RunBootstrap();

                Close();
            };
        }
    }
}