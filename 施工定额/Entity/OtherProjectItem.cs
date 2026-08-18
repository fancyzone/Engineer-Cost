using System.ComponentModel;

namespace 施工定额.Entity
{
    /// <summary>其他项目（暂列金额、暂估价、总承包服务费、计日工等）。</summary>
    public class OtherProjectItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string 名称 { get; set; } = "";

        private decimal _金额;
        public decimal 金额
        {
            get => _金额;
            set
            {
                if (_金额 == value) return;
                _金额 = value;
                OnPropertyChanged(nameof(金额));
            }
        }

        /// <summary>是否允许用户编辑金额（暂估价固定为 0）。</summary>
        public bool 可编辑 { get; set; } = true;
    }
}
