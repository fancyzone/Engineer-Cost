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
            Directory.CreateDirectory(AppConfig.DataDirectory);
            AppLogger.Info($"程序启动，版本 {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");

            ApplicationConfiguration.Initialize();

            bool startupOk = true;

            using (var bootstrap = new BootstrapForm())
            {
                bootstrap.RunBootstrap = async () =>
                {
                    await CheckAndApplyAppUpdateAsync();
                    await CheckAndApplyDbUpdateAsync();

                    try
                    {
                        AppCache.Instance.LoadAll();
                    }
                    catch (FileNotFoundException ex)
                    {
                        AppLogger.Error("启动失败：找不到数据库文件", ex);
                        MessageBox.Show(
                            $"启动失败，找不到必要的数据库文件。\n\n{ex.Message}\n\n请确认数据库文件与程序在同一目录下，或检查网络连接后重试。",
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

        private static async Task CheckAndApplyAppUpdateAsync()
        {
            string versionUrl = AppConfig.AppUpdateVersionInfoUrl;
            if (string.IsNullOrWhiteSpace(versionUrl))
                return;

            var updater = new AppUpdateService(versionUrl);
            AppVersionInfo? info = await updater.CheckForUpdateAsync();
            if (info == null)
                return;

            var choice = MessageBox.Show(
                $"检测到新版本程序\n当前版本：{AppUpdateService.GetCurrentVersion()}\n最新版本：{info.Version}\n{info.Remark}\n\n更新后程序会自动重启，是否现在更新？",
                "发现新版本",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (choice != DialogResult.Yes)
                return;

            using var progressForm = new UpdateProgressForm("正在更新程序", "正在下载最新版本...");
            progressForm.Show();
            progressForm.Refresh();

            try
            {
                var progress = new Progress<int>(p => progressForm.SetProgress(p));
                await updater.DownloadAndApplyAsync(info, progress, progressForm.Token);
            }
            catch (OperationCanceledException) when (progressForm.IsCancelledByUser)
            {
            }
            catch (Exception ex)
            {
                AppLogger.Error("更新程序失败", ex);
                MessageBox.Show(
                    $"更新程序失败：{ex.Message}\n\n将继续使用当前版本启动。",
                    "更新失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                progressForm.Close();
            }
        }

        private static async Task CheckAndApplyDbUpdateAsync()
        {
            CleanupStaleTempFiles();
            string versionUrl = AppConfig.UpdateVersionInfoUrl;
            if (string.IsNullOrWhiteSpace(versionUrl))
                return;

            string systemDbPath = AppConfig.SystemDbFilePath;
            var updater = new DbUpdateService(versionUrl, systemDbPath);

            VersionInfo? info = await updater.CheckForUpdateAsync();
            if (info == null)
                return;

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
            }
            catch (Exception ex)
            {
                AppLogger.Error("更新定额库失败", ex);
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
                foreach (var f in Directory.GetFiles(Path.GetTempPath(), "appupdate_*.zip"))
                    File.Delete(f);
            }
            catch
            {
            }
        }
    }
}
