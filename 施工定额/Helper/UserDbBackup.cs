namespace 施工定额.Helper
{
    /// <summary>
    /// 用户库简单备份：启动时按天保留一份副本到 backups 目录；支持列出与恢复。
    /// </summary>
    public static class UserDbBackup
    {
        private const int KeepDays = 7;

        public static string BackupDirectory =>
            Path.Combine(AppConfig.DataDirectory, "backups");

        public static void BackupIfNeeded(string userDbPath)
        {
            if (string.IsNullOrWhiteSpace(userDbPath) || !File.Exists(userDbPath))
                return;

            try
            {
                Directory.CreateDirectory(BackupDirectory);

                var todayStamp = DateTime.Now.ToString("yyyyMMdd");
                var todayFile = Path.Combine(BackupDirectory, $"userDB_{todayStamp}.db");
                if (!File.Exists(todayFile))
                {
                    File.Copy(userDbPath, todayFile, overwrite: false);
                    AppLogger.Info($"用户库已备份：{todayFile}");
                }

                CleanupOld(BackupDirectory);
            }
            catch (Exception ex)
            {
                AppLogger.Error("用户库备份失败", ex);
            }
        }

        /// <summary>按修改时间倒序列出备份文件。</summary>
        public static IReadOnlyList<FileInfo> ListBackups()
        {
            if (!Directory.Exists(BackupDirectory))
                return Array.Empty<FileInfo>();

            return Directory.GetFiles(BackupDirectory, "userDB_*.db")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();
        }

        /// <summary>
        /// 用指定备份覆盖当前用户库。调用前应关闭所有对该库的连接。
        /// 恢复前会再备份当前库一份（带时间戳），便于回滚。
        /// </summary>
        public static void Restore(string backupFilePath, string userDbPath)
        {
            if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
                throw new FileNotFoundException("备份文件不存在。", backupFilePath);

            if (string.IsNullOrWhiteSpace(userDbPath))
                throw new ArgumentException("用户库路径无效。", nameof(userDbPath));

            Directory.CreateDirectory(Path.GetDirectoryName(userDbPath)!);

            if (File.Exists(userDbPath))
            {
                var safety = Path.Combine(
                    BackupDirectory,
                    $"userDB_before_restore_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                Directory.CreateDirectory(BackupDirectory);
                File.Copy(userDbPath, safety, overwrite: true);
                AppLogger.Info($"恢复前已备份当前库：{safety}");
            }

            File.Copy(backupFilePath, userDbPath, overwrite: true);
            AppLogger.Info($"已从备份恢复用户库：{backupFilePath} -> {userDbPath}");
        }

        private static void CleanupOld(string backupDir)
        {
            var threshold = DateTime.Now.AddDays(-KeepDays);
            foreach (var file in Directory.GetFiles(backupDir, "userDB_*.db"))
            {
                try
                {
                    // 保留手动恢复前的 safety 备份更久一点也可，这里统一按天数清理
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
