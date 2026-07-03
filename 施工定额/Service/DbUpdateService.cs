using System.IO.Compression;
using System.Net.Http;
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
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

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
        /// 查询是否有新版本。网络异常时静默返回 null，不抛异常，不打断启动流程。
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

                return info.Version != GetLocalVersion() ? info : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 下载 zip、校验哈希、解压替换 systemDB.db。失败时自动回滚旧文件。
        /// 调用前必须保证 systemDB 没有被其他连接占用。
        /// </summary>
        public async Task DownloadAndApplyAsync(VersionInfo info, IProgress<int>? progress, CancellationToken ct = default)
        {
            string tempZip = Path.Combine(Path.GetTempPath(), $"systemDB_{Guid.NewGuid():N}.zip");

            using (var response = await _http.GetAsync(info.Url, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                response.EnsureSuccessStatusCode();
                long total = response.Content.Headers.ContentLength ?? -1L;

                await using var httpStream = await response.Content.ReadAsStreamAsync(ct);
                await using var fileStream = File.Create(tempZip);

                var buffer = new byte[81920];
                long readTotal = 0;
                int read;
                while ((read = await httpStream.ReadAsync(buffer, ct)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
                    readTotal += read;
                    if (total > 0)
                        progress?.Report((int)(readTotal * 100 / total));
                }
            }

            if (!string.IsNullOrWhiteSpace(info.Sha256))
            {
                using var sha = SHA256.Create();
                await using var checkStream = File.OpenRead(tempZip);
                var hash = Convert.ToHexString(await sha.ComputeHashAsync(checkStream, ct));
                if (!hash.Equals(info.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(tempZip);
                    throw new InvalidOperationException("下载文件校验失败（哈希不匹配），可能是文件损坏或被篡改。");
                }
            }

            string? targetDir = Path.GetDirectoryName(_systemDbPath);
            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);

            string backupPath = _systemDbPath + ".bak";
            bool hadOldDb = File.Exists(_systemDbPath);
            if (hadOldDb)
                File.Copy(_systemDbPath, backupPath, overwrite: true);

            try
            {
                using var archive = ZipFile.OpenRead(tempZip);
                var entry = archive.Entries.FirstOrDefault(e =>
                    e.Name.Equals(Path.GetFileName(_systemDbPath), StringComparison.OrdinalIgnoreCase));

                if (entry == null)
                    throw new InvalidOperationException("更新包内未找到 systemDB.db 文件，更新包可能已损坏。");

                entry.ExtractToFile(_systemDbPath, overwrite: true);
                File.WriteAllText(_localVersionFile, info.Version);
            }
            catch
            {
                if (hadOldDb && File.Exists(backupPath))
                    File.Copy(backupPath, _systemDbPath, overwrite: true);
                throw;
            }
            finally
            {
                if (File.Exists(tempZip)) File.Delete(tempZip);
                if (File.Exists(backupPath)) File.Delete(backupPath);
            }
        }
    }
}