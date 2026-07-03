using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace 施工定额.Service
{
    public class VersionInfo
    {
        public string Version { get; set; } = "";
        public string Url { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 负责检查并下载 systemDB.db 的更新包（zip 内应包含同名的 systemDB.db 文件）
    /// </summary>
    public class DbUpdateService
    {
        // 注意：不再依赖 HttpClient.Timeout 做整体下载超时的兜底，
        // 因为 stream 读取中途“卡死但连接未断开”时该超时经常不生效。
        // 改为下面自定义的 CancellationTokenSource 分层控制。
        private static readonly HttpClient _http = new() { Timeout = Timeout.InfiniteTimeSpan };

        private const int OverallTimeoutSeconds = 60;   // 整体下载最长等待时间
        private const int IdleReadTimeoutSeconds = 15;  // 单次读取“空闲多久算卡死”
        private const int CheckUpdateTimeoutSeconds = 15; // 检查版本信息的超时（体积小，可以短）
        private const int MaxRetryCount = 3;             // 下载失败重试次数

        private readonly string _versionInfoUrl;
        private readonly string _systemDbPath;
        private readonly string _localVersionFile;

        public DbUpdateService(string versionInfoUrl, string systemDbPath)
        {
            _versionInfoUrl = versionInfoUrl;
            _systemDbPath = systemDbPath;
            _localVersionFile = Path.Combine(
                Path.GetDirectoryName(systemDbPath) ?? AppContext.BaseDirectory,
                "systemDB.version.txt");
        }

        public string GetLocalVersion() =>
            File.Exists(_localVersionFile) ? File.ReadAllText(_localVersionFile).Trim() : "";

        /// <summary>
        /// 查询是否有新版本。网络异常/超时时静默返回 null，不抛异常，不打断启动流程。
        /// 支持 http(s):// 地址，也支持本地文件路径（方便本地测试）。
        /// </summary>
        public async Task<VersionInfo?> CheckForUpdateAsync(CancellationToken ct = default)
        {
            try
            {
                var json = await _http.GetStringAsync(_versionInfoUrl, ct);
                var info = JsonSerializer.Deserialize<VersionInfo>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (info == null || string.IsNullOrWhiteSpace(info.Version))
                    return null;

                // 版本号不一致，或者本地数据库文件已经不存在（比如被手动删除），
                // 都应该视为"需要更新"，而不是只看版本号字符串
                bool needsUpdate = info.Version != GetLocalVersion() || !File.Exists(_systemDbPath);

                return needsUpdate ? info : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 下载 zip、校验哈希、解压替换 systemDB.db。失败时自动回滚旧文件。
        /// 调用前必须保证 systemDB 没有被其他连接占用。
        ///
        /// 关键点：
        ///   - 不依赖 HttpClient.Timeout，改用自定义 CancellationTokenSource 分层控制：
        ///     ① 整体下载最长 OverallTimeoutSeconds 秒；
        ///     ② 每次 Stream.ReadAsync 单独限定 IdleReadTimeoutSeconds 秒“空闲超时”，
        ///        连接“假死”（连上但不发数据）时能可靠地打断，不会无限期挂起。
        ///   - 失败自动重试 MaxRetryCount 次（指数退避）。
        /// </summary>
        public async Task DownloadAndApplyAsync(VersionInfo info, IProgress<int>? progress, CancellationToken ct = default)
        {
            Exception? lastError = null;

            for (int attempt = 1; attempt <= MaxRetryCount; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    await DownloadAndApplyOnceAsync(info, progress, ct);
                    return; // 成功，直接返回
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    // 是内部超时触发的取消，不是用户主动取消 —— 可以重试
                    lastError = new TimeoutException("下载超时（网络连接卡住或中断）。");
                }
                catch (Exception ex) when (attempt < MaxRetryCount)
                {
                    lastError = ex;
                }

                if (attempt < MaxRetryCount)
                {
                    int delaySeconds = attempt * 3; // 简单的退避：3s, 6s...
                    progress?.Report(0);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
                }
            }

            throw lastError ?? new Exception("下载失败，原因未知。");
        }

        private async Task DownloadAndApplyOnceAsync(VersionInfo info, IProgress<int>? progress, CancellationToken ct)
        {
            string tempZip = Path.Combine(Path.GetTempPath(), $"systemDB_{Guid.NewGuid():N}.zip");

            using var overallCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            overallCts.CancelAfter(TimeSpan.FromSeconds(OverallTimeoutSeconds));

            try
            {
                using (var response = await _http.GetAsync(info.Url, HttpCompletionOption.ResponseHeadersRead, overallCts.Token))
                {
                    response.EnsureSuccessStatusCode();
                    long total = response.Content.Headers.ContentLength ?? -1L;

                    await using var httpStream = await response.Content.ReadAsStreamAsync(overallCts.Token);
                    await using var fileStream = File.Create(tempZip);

                    var buffer = new byte[81920];
                    long readTotal = 0;
                    int read;

                    while (true)
                    {
                        // 每次读取单独限定“空闲超时”：多久没有新字节进来就判定为卡死
                        using var readCts = CancellationTokenSource.CreateLinkedTokenSource(overallCts.Token);
                        readCts.CancelAfter(TimeSpan.FromSeconds(IdleReadTimeoutSeconds));

                        try
                        {
                            read = await httpStream.ReadAsync(buffer, readCts.Token);
                        }
                        catch (OperationCanceledException) when (!overallCts.Token.IsCancellationRequested)
                        {
                            // 只是这一次读取空闲超时，整体还没到 60 秒上限——视为卡死，直接判失败
                            throw new TimeoutException($"下载中断：{IdleReadTimeoutSeconds} 秒内未收到新数据。");
                        }

                        if (read <= 0) break;

                        await fileStream.WriteAsync(buffer.AsMemory(0, read), overallCts.Token);
                        readTotal += read;
                        if (total > 0)
                            progress?.Report((int)(readTotal * 100 / total));
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // 用户主动取消，原样抛出，不进入重试逻辑（外层 DownloadAndApplyAsync 里已排除这种情况）
                throw;
            }

            // ── 哈希校验 ──────────────────────────────────
            if (!string.IsNullOrWhiteSpace(info.Sha256))
            {
                string computedHash = "";
                await RunWithRetryAsync(async () =>
                {
                    using var sha = SHA256.Create();
                    await using var checkStream = File.OpenRead(tempZip);
                    computedHash = Convert.ToHexString(await sha.ComputeHashAsync(checkStream, ct));
                });

                // 顺手把可能存在的 "sha256:" 前缀去掉，兼容 version.json 写法不一致的情况
                string expectedHash = info.Sha256.Contains(':')
                    ? info.Sha256.Split(':').Last()
                    : info.Sha256;

                if (!computedHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(tempZip);
                    throw new InvalidOperationException("下载文件校验失败（哈希不匹配），可能是文件损坏或被篡改。");
                }
            }

            // ── 解压替换（带回滚）─────────────────────────
            string? targetDir = Path.GetDirectoryName(_systemDbPath);
            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);

            string backupPath = _systemDbPath + ".bak";
            bool hadOldDb = File.Exists(_systemDbPath);
            if (hadOldDb)
                File.Copy(_systemDbPath, backupPath, overwrite: true);

            try
            {
                await RunWithRetryAsync(async () =>
                {
                    using var archive = ZipFile.OpenRead(tempZip);
                    var entry = archive.Entries.FirstOrDefault(e =>
                        e.Name.Equals(Path.GetFileName(_systemDbPath), StringComparison.OrdinalIgnoreCase));

                    if (entry == null)
                        throw new InvalidOperationException("更新包内未找到 systemDB.db 文件，更新包可能已损坏。");

                    entry.ExtractToFile(_systemDbPath, overwrite: true);
                    await File.WriteAllTextAsync(_localVersionFile, info.Version, ct);
                });
            }
            catch
            {
                if (hadOldDb && File.Exists(backupPath))
                    File.Copy(backupPath, _systemDbPath, overwrite: true);
                throw;
            }
            finally
            {
                await RunWithRetryAsync(async () =>
                {
                    if (File.Exists(tempZip)) File.Delete(tempZip);
                    if (File.Exists(backupPath)) File.Delete(backupPath);
                    await Task.CompletedTask;
                });
            }
        }
        /// <summary>
        /// 文件刚写入磁盘后可能被杀毒软件/索引服务短暂扫描锁定，
        /// 这里对 IOException（文件被占用）做短暂重试，而不是直接失败。
        /// </summary>
        private static async Task<T> OpenWithRetryAsync<T>(Func<T> openFunc, CancellationToken ct)
        {
            const int maxAttempts = 5;
            const int delayMs = 300;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return openFunc();
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    await Task.Delay(delayMs, ct);
                }
            }

            // 最后一次不再吞异常，让真正的错误信息暴露出来
            return openFunc();
        }
        // DbUpdateService.cs 内新增一个私有辅助方法
        private static async Task RunWithRetryAsync(Func<Task> action, int maxAttempts = 5, int delayMs = 300)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    await action();
                    return;
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    await Task.Delay(delayMs * attempt); // 简单的线性退避
                }
            }
        }
    }
}