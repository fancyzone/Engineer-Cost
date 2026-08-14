using System.ComponentModel;

namespace 施工定额.Helper
{
    public static class BindingListExtensions
    {
        /// <summary>
        /// 批量替换 BindingList 内容，期间关闭列表变更通知，减少 DataGridView 闪烁与重绘。
        /// </summary>
        public static void ReplaceAll<T>(this BindingList<T> list, IEnumerable<T> items)
        {
            bool old = list.RaiseListChangedEvents;
            list.RaiseListChangedEvents = false;
            try
            {
                list.Clear();
                foreach (var item in items)
                    list.Add(item);
            }
            finally
            {
                list.RaiseListChangedEvents = old;
                list.ResetBindings();
            }
        }
    }
}
