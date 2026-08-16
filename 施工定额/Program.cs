using 施工定额.Helper;

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

            bool startupOk = true;

            using (var bootstrap = new BootstrapForm())
            {
                bootstrap.RunBootstrap = async () =>
                {
                    // systemDB 随程序包发布，启动时不再在线检查/下载定额库
                    await Task.CompletedTask;

                    try
                    {
                        AppCache.Instance.LoadAll();
                    }
                    catch (FileNotFoundException ex)
                    {
                        AppLogger.Error("启动失败：找不到数据库文件", ex);
                        MessageBox.Show(
                            $"启动失败，找不到必要的数据库文件。\n\n{ex.Message}\n\n请确认数据库文件与程序在同一目录下。",
                            "启动错误",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        startupOk = false;
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error("启动时加载数据失败", ex);
                        MessageBox.Show(
                            $"启动时加载数据失败：\n\n{ex.Message}\n\n请检查数据库文件是否损坏或被其他程序占用。",
                            "启动错误",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        startupOk = false;
                    }
                };

                Application.Run(bootstrap);
            }

            if (startupOk)
                Application.Run(new Form1());
        }
    }
}
