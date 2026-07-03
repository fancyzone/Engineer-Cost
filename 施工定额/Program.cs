using 施工定额.Helper;
using 施工定额.Service;
using 施工定额.UI;

namespace 施工定额
{
    internal static class Program
    {
        [STAThread]
        static async Task Main()
        {
            var dataDir = Path.Combine(
       Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
       "施工定额");
            Directory.CreateDirectory(dataDir);

            ApplicationConfiguration.Initialize();

            // 用一个“启动引导窗口”来驱动异步更新检查，
            // 保证 Application.Run 已经在跑消息泵的情况下再执行下载逻辑。
            bool startupOk = true;

            using (var bootstrap = new BootstrapForm())
            {
                bootstrap.RunBootstrap = async () =>
                {
                    await CheckAndApplyDbUpdateAsync();

                    try
                    {
                        AppCache.Instance.LoadAll();
                    }
                    catch (FileNotFoundException ex)
                    {
                        MessageBox.Show(
                            $"启动失败，找不到必要的数据库文件。\n\n{ex.Message}\n\n请确认数据库文件与程序在同一目录下，或检查网络连接后重试。",
                            "启动错误",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        startupOk = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"启动时加载数据失败：\n\n{ex.Message}\n\n请检查数据库文件是否损坏或被其他程序占用。",
                            "启动错误",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        startupOk = false;
                    }
                };

                Application.Run(bootstrap); // 消息泵从这里开始跑，bootstrap 完成后自动 Close()
            }

            if (startupOk)
                Application.Run(new Form1());
        }

        private static async Task CheckAndApplyDbUpdateAsync()
        {
            CleanupStaleTempFiles();
            string versionUrl = AppConfig.UpdateVersionInfoUrl;
            if (string.IsNullOrWhiteSpace(versionUrl))
                return; // 未配置更新地址，功能关闭

            string systemDbPath = AppConfig.SystemDbFilePath;
            var updater = new DbUpdateService(versionUrl, systemDbPath);

            VersionInfo? info = await updater.CheckForUpdateAsync();
            if (info == null)
                return; // 无更新，或联网失败——都静默跳过

            bool dbMissing = !File.Exists(systemDbPath);

            if (!dbMissing)
            {
                var choice = MessageBox.Show(
                    $"检测到新版定额库\n当前版本：{(string.IsNullOrEmpty(updater.GetLocalVersion()) ? "未知" : updater.GetLocalVersion())}\n最新版本：{info.Version}\n{info.Remark}\n\n是否现在更新？",
                    "发现更新",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (choice != DialogResult.Yes)
                    return;
            }
            // 首次运行缺库：不打扰用户，直接尝试静默下载

            using var progressForm = new UpdateProgressForm();
            progressForm.Show();
            progressForm.Refresh();

            try
            {
                var progress = new Progress<int>(p => progressForm.SetProgress(p));
                await updater.DownloadAndApplyAsync(info, progress, progressForm.Token);
            }
            catch (OperationCanceledException) when (progressForm.IsCancelledByUser)
            {
                // 用户主动点了取消，不算错误，静默跳过即可
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"更新定额库失败：{ex.Message}\n\n将尝试使用现有数据继续启动。",
                    "更新失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                progressForm.Close();
            }
        }
        private static void CleanupStaleTempFiles()
        {
            try
            {
                foreach (var f in Directory.GetFiles(Path.GetTempPath(), "systemDB_*.zip"))
                    File.Delete(f);
            }
            catch
            {
                // 忽略清理失败（比如文件正被占用），不影响主流程
            }
        }
    }
}