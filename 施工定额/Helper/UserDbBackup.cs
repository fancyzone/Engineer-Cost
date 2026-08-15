namespace 施工定额.Helper
{
    /// <summary>
    /// 用户库简单备份：启动时按天保留一份副本到 backups 目录。
    /// </summary>
    public static class UserDbBackup
    {
        private const int KeepDays = 7;

        public static void BackupIfNeeded(string userDbPath)
        {
            if (string.IsNullOrWhiteSpace(userDbPath) || !File.Exists(userDbPath))
                return;

            try
            {
                var backupDir = Path.Combine(AppConfig.DataDirectory, "backups");
                Directory.CreateDirectory(backupDir);

                var todayStamp = DateTime.Now.ToString("yyyyMMdd");
                var todayFile = Path.Combine(backupDir, $"userDB_{todayStamp}.db");
                if (!File.Exists(todayFile))
                {
                    File.Copy(userDbPath, todayFile, overwrite: false);
                    AppLogger.Info($"用户库已备份：{todayFile}");
                }

                CleanupOld(backupDir);
            }
            catch (Exception ex)
            {
                AppLogger.Error("用户库备份失败", ex);
            }
        }

        private static void CleanupOld(string backupDir)
        {
            var threshold = DateTime.Now.AddDays(-KeepDays);
            foreach (var file in Directory.GetFiles(backupDir, "userDB_*.db"))
            {
                try
                {
                    if (File.GetLastWriteTime(file) < threshold)
                        File.Delete(file);
                }
                catch
                {
                }
            }
        }
    }
}
