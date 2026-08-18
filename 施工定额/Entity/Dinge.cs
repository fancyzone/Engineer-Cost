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
        private decimal _换算系数 = 1m;
        private string _定额名称 = "";

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

        /// <summary>
        /// 清单工程量 → 定额工程量 的换算系数（默认 1）。
        /// 持久化到用户库「定额_市政工程.换算系数」。
        /// </summary>
        public decimal 换算系数
        {
            get => _换算系数;
            set
            {
                if (_换算系数 != value)
                {
                    _换算系数 = value == 0 ? 1m : value;
                    OnPropertyChanged(nameof(换算系数));
                }
            }
        }

        public int 分类ID { get; set; }
        public string ID号 { get; set; } = "";
        public string 清单编码 { get; set; } = "";
        public string 定额编码 { get; set; } = "";

        public string 定额名称
        {
            get => _定额名称;
            set
            {
                var v = value ?? "";
                if (_定额名称 != v)
                {
                    _定额名称 = v;
                    OnPropertyChanged(nameof(定额名称));
                }
            }
        }

        public string 定额单位 { get; set; } = "";

        public List<Xiaohaoliang> 消耗量列表 { get; set; } = new List<Xiaohaoliang>();

        /// <summary>
        /// 运行时费用构成（人材机、管理费、利润等），不持久化到数据库。
        /// </summary>
        public CostBreakdown 费用构成 { get; set; } = new CostBreakdown();

        public override string ToString() =>
            $"{ID号} - {定额编码} - {定额名称} - {定额工程量} - {定额单价} - {定额合价}";
    }
}
