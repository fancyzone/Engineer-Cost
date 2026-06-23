using System.Reflection;
using System.Windows.Forms;

namespace 施工定额.Helper
{
    public static class ControlExtensions
    {
        public static void SetDoubleBuffered(this Control control, bool value = true)
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(control, value, null);
        }
    }
}