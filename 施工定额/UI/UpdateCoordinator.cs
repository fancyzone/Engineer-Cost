using 施工定额.Helper;
using 施工定额.Service;

namespace 施工定额.UI
{
    /// <summary>
    /// 手动检查更新：程序本体 + 定额库。
    /// 从启动流程拆出，由工具栏「检查更新」触发。
    /// </summary>
    public static class UpdateCoordinator
    {
        /// <summary>
        /// 检查并可选应用程序与定额库更新。
        /// </summary>
        /// <param name="owner">父窗口</param>
        /// <param name="silentIfUpToDate">无更新时是否静默；工具栏点击时应为 false 以提示「已是最新」</param>
        public static async Task CheckAllAsync(IWin32Window? owner, bool silentIfUpToDate = false)
        {
            CleanupStaleTempFiles();

            bool appChecked = await CheckAndApplyAppUpdateAsync(owner, silentIfUpToDate);
            bool dbChecked = await CheckAndApplyDbUpdateAsync(owner, silentIfUpToDate, forcePromptIfMissing: false);

            if (!silentIfUpToDate && !appChecked && !dbChecked)
            {
                MessageBox.Show(
                    owner,
                    $"当前程序版本：{AppUpdateService.GetCurrentVersion()}\n\n程序与定额库均为最新，无需更新。",
                    "检查更新",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// 仅在系统库文件缺失时静默下载（首次安装/数据目录被清空）。
        /// </summary>
        public static async Task EnsureSystemDbPresentAsync()
        {
            CleanupStaleTempFiles();

            string systemDbPath = AppConfig.SystemDbFilePath;
            if (File.Exists(systemDbPath))
                return;

            string versionUrl = AppConfig.UpdateVersionInfoUrl;
            if (string.IsNullOrWhiteSpace(versionUrl))
                return;

            var updater = new DbUpdateService(versionUrl, systemDbPath);
            VersionInfo? info = await updater.CheckForUpdateAsync();
            if (info == null)
                return;

            using var progressForm = new UpdateProgressForm("正在下载定额库", "首次运行，正在获取定额库...");
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
                AppLogger.Error("首次下载定额库失败", ex);
                MessageBox.Show(
                    $"下载定额库失败：{ex.Message}\n\n请检查网络后，通过工具栏「检查更新」重试。",
                    "下载失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                progressForm.Close();
            }
        }

        private static async Task<bool> CheckAndApplyAppUpdateAsync(IWin32Window? owner, bool silentIfUpToDate)
        {
            string versionUrl = AppConfig.AppUpdateVersionInfoUrl;
            if (string.IsNullOrWhiteSpace(versionUrl))
                return false;

            var updater = new AppUpdateService(versionUrl);
            AppVersionInfo? info;
            try
            {
                info = await updater.CheckForUpdateAsync();
            }
            catch (Exception ex)
            {
                AppLogger.Error("检查程序更新失败", ex);
                if (!silentIfUpToDate)
                {
                    MessageBox.Show(
                        owner,
                        $"检查程序更新失败：{ex.Message}",
                        "检查更新",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                return false;
            }

            if (info == null)
                return false;

            var choice = MessageBox.Show(
                owner,
                $"检测到新版本程序\n当前版本：{AppUpdateService.GetCurrentVersion()}\n最新版本：{info.Version}\n{info.Remark}\n\n更新后程序会自动重启，是否现在更新？",
                "发现新版本",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (choice != DialogResult.Yes)
                return true;

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
                    owner,
                    $"更新程序失败：{ex.Message}",
                    "更新失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                progressForm.Close();
            }

            return true;
        }

        private static async Task<bool> CheckAndApplyDbUpdateAsync(
            IWin32Window? owner,
            bool silentIfUpToDate,
            bool forcePromptIfMissing)
        {
            string versionUrl = AppConfig.UpdateVersionInfoUrl;
            if (string.IsNullOrWhiteSpace(versionUrl))
                return false;

            string systemDbPath = AppConfig.SystemDbFilePath;
            var updater = new DbUpdateService(versionUrl, systemDbPath);

            VersionInfo? info;
            try
            {
                info = await updater.CheckForUpdateAsync();
            }
            catch (Exception ex)
            {
                AppLogger.Error("检查定额库更新失败", ex);
                if (!silentIfUpToDate)
                {
                    MessageBox.Show(
                        owner,
                        $"检查定额库更新失败：{ex.Message}",
                        "检查更新",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                return false;
            }

            if (info == null)
                return false;

            bool dbMissing = !File.Exists(systemDbPath);

            if (!dbMissing || forcePromptIfMissing)
            {
                var choice = MessageBox.Show(
                    owner,
                    $"检测到新版定额库\n当前版本：{(string.IsNullOrEmpty(updater.GetLocalVersion()) ? "未知" : updater.GetLocalVersion())}\n最新版本：{info.Version}\n{info.Remark}\n\n是否现在更新？",
                    "发现更新",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (choice != DialogResult.Yes)
                    return true;
            }

            using var progressForm = new UpdateProgressForm();
            progressForm.Show();
            progressForm.Refresh();

            try
            {
                var progress = new Progress<int>(p => progressForm.SetProgress(p));
                await updater.DownloadAndApplyAsync(info, progress, progressForm.Token);

                if (!silentIfUpToDate)
                {
                    MessageBox.Show(
                        owner,
                        "定额库已更新。部分数据可能需要重新打开定额库窗口后生效。",
                        "更新完成",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (OperationCanceledException) when (progressForm.IsCancelledByUser)
            {
            }
            catch (Exception ex)
            {
                AppLogger.Error("更新定额库失败", ex);
                MessageBox.Show(
                    owner,
                    $"更新定额库失败：{ex.Message}",
                    "更新失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                progressForm.Close();
            }

            return true;
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
