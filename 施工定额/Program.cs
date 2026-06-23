using 施工定额.Helper;

namespace 施工定额
{
    internal static class Program
    {

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 确保数据目录存在
            var dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "施工定额");
            Directory.CreateDirectory(dataDir); // 目录已存在时此方法不报错
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            AppCache.Instance.LoadAll(); // ← 加这一行，启动时统一加载
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}