using 施工定额.Helper;
using 施工定额.Service;

namespace 施工定额.UI
{
    /// <summary>
    /// 手动检查程序更新（定额库 systemDB 随程序包发布，不再单独在线更新）。
    /// 由工具栏「检查更新」触发。
    /// </summary>
    public static class UpdateCoordinator
    {
        /// <summary>
        /// 检查并可选安装程序更新。
        /// </summary>
        /// <param name="owner">父窗口</param>
        /// <param name="silentIfUpToDate">无更新时是否静默；工具栏点击时应为 false 以提示「已是最新」</param>
        public static async Task CheckAllAsync(IWin32Window? owner, bool silentIfUpToDate = false)
        {
            CleanupStaleTempFiles();

            bool appChecked = await CheckAndApplyAppUpdateAsync(owner, silentIfUpToDate);

            if (!silentIfUpToDate && !appChecked)
            {
                MessageBox.Show(
                    owner,
                    $"当前程序版本：{AppUpdateService.GetCurrentVersion()}\n\n已是最新版本，无需更新。",
                    "检查更新",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        /// <returns>true 表示检测到更新（不论用户是否选择安装）</returns>
        private static async Task<bool> CheckAndApplyAppUpdateAsync(IWin32Window? owner, bool silentIfUpToDate)
        {
            string versionUrl = AppConfig.AppUpdateVersionInfoUrl;
            if (string.IsNullOrWhiteSpace(versionUrl))
            {
                if (!silentIfUpToDate)
                {
                    MessageBox.Show(
                        owner,
                        "未配置程序更新地址（AppVersionInfoUrl），无法检查更新。",
                        "检查更新",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                return false;
            }

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

        private static void CleanupStaleTempFiles()
        {
            try
            {
                foreach (var f in Directory.GetFiles(Path.GetTempPath(), "appupdate_*.zip"))
                    File.Delete(f);
            }
            catch
            {
            }
        }
    }
}
