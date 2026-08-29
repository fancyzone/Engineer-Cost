using System.Text.Json;
using 施工定额.Helper;

namespace 施工定额.UI
{
    /// <summary>
    /// 记忆 DataGridView 列宽到 %AppData%\施工定额\grid_layout.json。
    /// </summary>
    public static class GridLayoutStore
    {
        private static readonly object _lock = new();
        private static Dictionary<string, Dictionary<string, int>>? _cache;
        private static readonly HashSet<DataGridView> _attached = new();
        private static System.Windows.Forms.Timer? _saveTimer;
        private static bool _suppress;

        private static string FilePath => Path.Combine(AppConfig.DataDirectory, "grid_layout.json");

        public static void Attach(DataGridView dgv)
        {
            if (dgv == null) return;
            if (string.IsNullOrEmpty(dgv.Name))
                dgv.Name = "grid_" + dgv.GetHashCode().ToString("X");

            Restore(dgv);

            lock (_lock)
            {
                if (_attached.Contains(dgv)) return;
                _attached.Add(dgv);
            }

            dgv.ColumnWidthChanged += OnColumnWidthChanged;
            dgv.Disposed += (_, _) =>
            {
                lock (_lock) _attached.Remove(dgv);
            };
        }

        public static void Restore(DataGridView dgv)
        {
            var map = Load();
            if (!map.TryGetValue(dgv.Name, out var widths) || widths.Count == 0)
                return;

            _suppress = true;
            try
            {
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (string.IsNullOrEmpty(col.Name)) continue;
                    if (widths.TryGetValue(col.Name, out int w) && w >= 20 && w <= 2000)
                        col.Width = w;
                }
            }
            finally
            {
                _suppress = false;
            }
        }

        private static void OnColumnWidthChanged(object? sender, DataGridViewColumnEventArgs e)
        {
            if (_suppress) return;
            if (sender is not DataGridView) return;
            ScheduleSave();
        }

        private static void ScheduleSave()
        {
            if (_saveTimer == null)
            {
                _saveTimer = new System.Windows.Forms.Timer { Interval = 400 };
                _saveTimer.Tick += (_, _) =>
                {
                    _saveTimer.Stop();
                    SaveAll();
                };
            }
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        public static void SaveAll()
        {
            Dictionary<string, Dictionary<string, int>> snapshot;
            lock (_lock)
            {
                snapshot = Load();
                foreach (var dgv in _attached.ToList())
                {
                    if (dgv.IsDisposed) continue;
                    var widths = new Dictionary<string, int>(StringComparer.Ordinal);
                    foreach (DataGridViewColumn col in dgv.Columns)
                    {
                        if (string.IsNullOrEmpty(col.Name)) continue;
                        if (col.Visible && col.Width > 0)
                            widths[col.Name] = col.Width;
                    }
                    if (widths.Count > 0)
                        snapshot[dgv.Name] = widths;
                }
                _cache = snapshot;
            }

            try
            {
                Directory.CreateDirectory(AppConfig.DataDirectory);
                var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // 写失败不影响主流程
            }
        }

        private static Dictionary<string, Dictionary<string, int>> Load()
        {
            lock (_lock)
            {
                if (_cache != null)
                    return _cache;

                _cache = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
                try
                {
                    if (!File.Exists(FilePath))
                        return _cache;
                    var json = File.ReadAllText(FilePath);
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(json);
                    if (parsed != null)
                        _cache = new Dictionary<string, Dictionary<string, int>>(parsed, StringComparer.Ordinal);
                }
                catch
                {
                    // 损坏则忽略，下次覆盖
                }
                return _cache;
            }
        }
    }
}
