using System.ComponentModel;

namespace 施工定额.Entity
{
    public class Dinge : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private decimal _定额工程量;
        private decimal _定额单价;
        private decimal _定额合价;

        public decimal 定额工程量
        {
            get => _定额工程量;
            set
            {
                if (_定额工程量 != value)
                {
                    _定额工程量 = value;
                    OnPropertyChanged(nameof(定额工程量));
                }
            }
        }

        public decimal 定额单价
        {
            get => _定额单价;
            set
            {
                if (_定额单价 != value)
                {
                    _定额单价 = value;
                    OnPropertyChanged(nameof(定额单价));
                }
            }
        }

        public decimal 定额合价
        {
            get => _定额合价;
            set
            {
                if (_定额合价 != value)
                {
                    _定额合价 = value;
                    OnPropertyChanged(nameof(定额合价));
                }
            }
        }

        public int 分类ID { get; set; }
        public string ID号 { get; set; }
        public string 清单编码 { get; set; }
        public string 定额编码 { get; set; }
        public string 定额名称 { get; set; }
        public string 定额单位 { get; set; }

        public List<Xiaohaoliang> 消耗量列表 { get; set; } = new List<Xiaohaoliang>();

        public override string ToString() =>
            $"{ID号} - {定额编码} - {定额名称} - {定额工程量} - {定额单价} - {定额合价}";
    }
}