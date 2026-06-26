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
            var dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "施工定额");
                    Directory.CreateDirectory(dataDir);

            ApplicationConfiguration.Initialize();

            try
            {
                AppCache.Instance.LoadAll();
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show(
                    $"启动失败，找不到必要的数据库文件。\n\n{ex.Message}\n\n请确认数据库文件与程序在同一目录下。",
                    "启动错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return; // 干净退出，不进入消息循环
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"启动时加载数据失败：\n\n{ex.Message}\n\n请检查数据库文件是否损坏或被其他程序占用。",
                    "启动错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new Form1());
        }
    }
}