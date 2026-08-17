using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace 施工定额.Service
{
    public class AppVersionInfo
    {
        public string Version { get; set; } = "";
        public string Url { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 负责检查并应用程序本体更新（exe/dll）。
    /// systemDB 随程序包分发，不再单独在线更新。
    ///
    /// Windows 下运行中的 exe 不能覆盖自己，采用「外部脚本接力」方案：
    ///   1. 下载新版本 zip 并校验哈希
    ///   2. 解压到临时目录
    ///   3. 生成 .bat：等本进程退出 → 覆盖安装目录 → 重启程序 → 自我删除
    ///   4. 启动脚本后退出当前进程
    /// </summary>
    public class AppUpdateService
    {
        private static readonly HttpClient _http = new() { Timeout = Timeout.InfiniteTimeSpan };

        private const int OverallTimeoutSeconds = 120;
        private const int IdleReadTimeoutSeconds = 15;
        private const int MaxRetryCount = 3;

        private readonly string _versionInfoUrl;

        public AppUpdateService(string versionInfoUrl)
        {
            _versionInfoUrl = versionInfoUrl;
        }

        /// <summary>当前程序版本，取自 csproj 里的 &lt;Version&gt;</summary>
        public static Version GetCurrentVersion() =>
            System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);

        /// <summary>
        /// 判断远程版本字符串是否新于当前版本（可单测）。
        /// 解析失败视为不需要更新。
        /// </summary>
        public static bool IsNewerThanCurrent(string? remoteVersionText, Version? current = null)
        {
            if (string.IsNullOrWhiteSpace(remoteVersionText))
                return false;
            if (!Version.TryParse(remoteVersionText.Trim(), out var remote))
                return false;
            current ??= GetCurrentVersion();
            return remote > current;
        }

        /// <summary>
        /// 查询是否有新版本。网络异常/超时/解析失败时静默返回 null。
        /// </summary>
        public async Task<AppVersionInfo?> CheckForUpdateAsync(CancellationToken ct = default)
        {
            try
            {
                var json = await _http.GetStringAsync(_versionInfoUrl, ct);
                var info = JsonSerializer.Deserialize<AppVersionInfo>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (info == null || string.IsNullOrWhiteSpace(info.Version) || string.IsNullOrWhiteSpace(info.Url))
                    return null;

                return IsNewerThanCurrent(info.Version) ? info : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 下载、校验、准备更新脚本并启动它，然后退出当前进程。
        /// 成功时不会返回——调用后当前进程会被 Environment.Exit 终止。
        /// </summary>
        public async Task DownloadAndApplyAsync(AppVersionInfo info, IProgress<int>? progress, CancellationToken ct = default)
        {
            Exception? lastError = null;
            string? extractDir = null;

            for (int attempt = 1; attempt <= MaxRetryCount; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    extractDir = await DownloadAndExtractOnceAsync(info, progress, ct);
                    break;
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    lastError = new TimeoutException("下载超时（网络连接卡住或中断）。");
                }
                catch (Exception ex) when (attempt < MaxRetryCount)
                {
                    lastError = ex;
                }

                if (extractDir == null && attempt < MaxRetryCount)
                {
                    progress?.Report(0);
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 3), ct);
                }
            }

            if (extractDir == null)
                throw lastError ?? new Exception("下载失败，原因未知。");

            LaunchUpdaterAndExit(extractDir);
        }

        private async Task<string> DownloadAndExtractOnceAsync(AppVersionInfo info, IProgress<int>? progress, CancellationToken ct)
        {
            string tempZip = Path.Combine(Path.GetTempPath(), $"appupdate_{Guid.NewGuid():N}.zip");

            using var overallCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            overallCts.CancelAfter(TimeSpan.FromSeconds(OverallTimeoutSeconds));

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
                    using var readCts = CancellationTokenSource.CreateLinkedTokenSource(overallCts.Token);
                    readCts.CancelAfter(TimeSpan.FromSeconds(IdleReadTimeoutSeconds));

                    try
                    {
                        read = await httpStream.ReadAsync(buffer, readCts.Token);
                    }
                    catch (OperationCanceledException) when (!overallCts.Token.IsCancellationRequested)
                    {
                        throw new TimeoutException($"下载中断：{IdleReadTimeoutSeconds} 秒内未收到新数据。");
                    }

                    if (read <= 0) break;

                    await fileStream.WriteAsync(buffer.AsMemory(0, read), overallCts.Token);
                    readTotal += read;
                    if (total > 0)
                        progress?.Report((int)(readTotal * 100 / total));
                }
            }

            if (!string.IsNullOrWhiteSpace(info.Sha256))
            {
                using var sha = SHA256.Create();
                await using var checkStream = File.OpenRead(tempZip);
                string computedHash = Convert.ToHexString(await sha.ComputeHashAsync(checkStream, ct));
                string expectedHash = info.Sha256.Contains(':') ? info.Sha256.Split(':').Last() : info.Sha256;

                if (!computedHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(tempZip);
                    throw new InvalidOperationException("安装包校验失败（哈希不匹配），可能已损坏或被篡改。");
                }
            }

            string extractDir = Path.Combine(Path.GetTempPath(), $"appupdate_extract_{Guid.NewGuid():N}");
            Directory.CreateDirectory(extractDir);
            ZipFile.ExtractToDirectory(tempZip, extractDir, overwriteFiles: true);
            File.Delete(tempZip);

            return extractDir;
        }

        private void LaunchUpdaterAndExit(string extractDir)
        {
            string installDir = AppContext.BaseDirectory.TrimEnd('\\');
            string exePath = Environment.ProcessPath ?? Path.Combine(installDir, "施工定额.exe");
            int pid = Environment.ProcessId;

            string scriptPath = Path.Combine(Path.GetTempPath(), $"apply_update_{Guid.NewGuid():N}.bat");

            // /XF 排除：用户库、可能被占用的 systemDB、本地 appsettings
            string script = $@"
                    @echo off
                    setlocal
                    :waitloop
                    tasklist /FI ""PID eq {pid}"" 2>NUL | find "" {pid} "" >NUL
                    if not errorlevel 1 (
                        timeout /t 1 /nobreak >NUL
                        goto waitloop
                    )
                    robocopy ""{extractDir}"" ""{installDir}"" /E /IS /IT /XF userDB.db systemDB.db systemDB.version.txt appsettings.json
                    start """" ""{exePath}""
                    rmdir /S /Q ""{extractDir}""
                    del ""%~f0""
                    ";

            File.WriteAllText(scriptPath, script, System.Text.Encoding.Default);

            var psi = new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };
            Process.Start(psi);

            Environment.Exit(0);
        }
    }
}
