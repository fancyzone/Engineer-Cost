using System.ComponentModel;

namespace 施工定额.Entity
{
    public class Xiaohaoliang : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private decimal _市场价;
        private decimal _含量;
        private decimal _数量;
        private decimal _市场价合计;

        public decimal 市场价
        {
            get => _市场价;
            set
            {
                if (_市场价 != value)
                {
                    _市场价 = value;
                    OnPropertyChanged(nameof(市场价));
                }
            }
        }

        public decimal 含量
        {
            get => _含量;
            set
            {
                if (_含量 != value)
                {
                    _含量 = value;
                    OnPropertyChanged(nameof(含量));
                }
            }
        }

        public decimal 数量
        {
            get => _数量;
            set
            {
                if (_数量 != value)
                {
                    _数量 = value;
                    OnPropertyChanged(nameof(数量));
                }
            }
        }

        public decimal 市场价合计
        {
            get => _市场价合计;
            set
            {
                if (_市场价合计 != value)
                {
                    _市场价合计 = value;
                    OnPropertyChanged(nameof(市场价合计));
                }
            }
        }

        public string 定额ID { get; set; } = "";
        public string 清单编码 { get; set; } = "";
        public string 定额编码 { get; set; } = "";
        public string 消耗量类别 { get; set; } = "";
        public string 消耗量编码 { get; set; } = "";
        public string 消耗量名称 { get; set; } = "";
        public string 规格型号 { get; set; } = "";
        public string 消耗量单位 { get; set; } = "";
        public decimal 定额基价 { get; set; }

        public override string ToString() =>
            $"{定额ID} - {消耗量类别} - {消耗量编码} - {消耗量名称} - {规格型号} - {消耗量单位} - {含量} - {数量} - {定额基价} - {市场价} - {市场价合计}";
    }
}
