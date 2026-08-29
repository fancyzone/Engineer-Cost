using 施工定额.Helper;

using 施工定额.UI;

namespace 施工定额
{
    internal static class Program
    {
        /// <summary>
        /// 必须使用同步 void Main + [STAThread]。
        /// async Task Main 会导致入口线程不是 STA，SaveFileDialog / OpenFileDialog 等 OLE 对话框会抛 ThreadStateException。
        /// 异步启动逻辑放在 BootstrapForm.RunBootstrap 中，由 UI 消息循环调度。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Directory.CreateDirectory(AppConfig.DataDirectory);
            AppLogger.Info($"程序启动，版本 {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");

            ApplicationConfiguration.Initialize();
            UiTheme.ApplyApplicationDefaults();

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
            {
                AppLogger.Error("UI 线程未处理异常", e.Exception);
                MessageBox.Show(e.Exception.ToString(), "未处理异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                AppLogger.Error("非 UI 线程未处理异常", ex);
                try
                {
                    MessageBox.Show(
                        ex?.ToString() ?? e.ExceptionObject?.ToString() ?? "未知错误",
                        "严重错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                catch { /* 忽略 */ }
            };

            try
            {
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                AppLogger.Error("Main 顶层异常", ex);
                MessageBox.Show(
                    ex.ToString(),
                    "启动失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
