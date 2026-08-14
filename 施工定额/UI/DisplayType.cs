namespace 施工定额.UI
{
    /// <summary>
    /// 主界面三层表格的刷新目标。
    /// 从 Form1 中抽出，避免 Presenter / ContextMenuBuilder 反向依赖窗体类型。
    /// </summary>
    public enum DisplayType
    {
        Qingdan,
        Dinge,
        Xiaohaoliang
    }
}
