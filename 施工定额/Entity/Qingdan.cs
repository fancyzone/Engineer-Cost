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
        private string _清单名称 = "";
        private string _项目特征 = "";

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

        public string 清单编码 { get; set; } = "";

        public string 清单名称
        {
            get => _清单名称;
            set
            {
                var v = value ?? "";
                if (_清单名称 != v)
                {
                    _清单名称 = v;
                    OnPropertyChanged(nameof(清单名称));
                }
            }
        }

        public string 项目特征
        {
            get => _项目特征;
            set
            {
                var v = value ?? "";
                if (_项目特征 != v)
                {
                    _项目特征 = v;
                    OnPropertyChanged(nameof(项目特征));
                }
            }
        }

        public string 单位 { get; set; } = "";
        public int Level { get; set; }

        /// <summary>
        /// 分部分项 / 措施项目 / 其他项目。见 <see cref="QingdanCategory"/>。
        /// </summary>
        public string 项目类别 { get; set; } = QingdanCategory.分部分项;

        /// <summary>所属单位工程编码，对应「单位工程」表。</summary>
        public string 单位工程编码 { get; set; } = "";

        public List<Dinge> 定额列表 { get; set; } = new List<Dinge>();

        /// <summary>
        /// 运行时费用构成（下属定额汇总），不持久化到数据库。
        /// </summary>
        public CostBreakdown 费用构成 { get; set; } = new CostBreakdown();

        public override string ToString() =>
            $"{清单编码} - {清单名称} - {工程量} - {综合单价} - {综合合价}";
    }
}
