using System.ComponentModel;

namespace 施工定额.Entity
{
    public class Qingdan : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private decimal _工程量;
        private decimal _综合单价;
        private decimal _综合合价;

        public decimal 工程量
        {
            get => _工程量;
            set
            {
                if (_工程量 != value)
                {
                    _工程量 = value;
                    OnPropertyChanged(nameof(工程量));
                }
            }
        }
        public decimal 综合单价
        {
            get => _综合单价;
            set
            {
                if (_综合单价 != value)
                {
                    _综合单价 = value;
                    OnPropertyChanged(nameof(综合单价));
                }
            }
        }

        public decimal 综合合价
        {
            get => _综合合价;
            set
            {
                if (_综合合价 != value)
                {
                    _综合合价 = value;
                    OnPropertyChanged(nameof(综合合价));
                }
            }
        }
        public string 清单编码 { get; set; }
        public string 清单名称 { get; set; }
        public string 项目特征 { get; set; }
        public string 单位 { get; set; }
        public int Level { get; set; }
        public List<Dinge> 定额列表 { get; set; } = new List<Dinge>();

        public override string ToString() =>
            $"{清单编码} - {清单名称} - {工程量} - {综合单价} - {综合合价}";
    }
}