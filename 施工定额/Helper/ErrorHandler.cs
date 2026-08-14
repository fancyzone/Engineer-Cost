namespace 施工定额.Helper
{
    /// <summary>
    /// 统一异常提示与日志记录，避免各处直接 MessageBox.Show(ex.Message)。
    /// </summary>
    public static class ErrorHandler
    {
        public static void Show(Exception ex, string title = "操作失败")
        {
            AppLogger.Error(title, ex);
            var msg = string.IsNullOrWhiteSpace(ex.Message)
                ? "发生未知错误，请查看日志获取详细信息。"
                : ex.Message;
            MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void ShowBusiness(string message, string title = "提示")
        {
            AppLogger.Info($"{title}: {message}");
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void ShowError(string message, string title = "错误")
        {
            AppLogger.Error(message);
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
