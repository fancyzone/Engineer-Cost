namespace 施工定额.Entity
{
    public class Xiaohaoliang
    {
        private decimal _市场价;
        private decimal _含量;
        public decimal 市场价
        {
            get => _市场价;
            set
            {
                if (_市场价 != value)
                {
                    _市场价 = value;
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
                }
            }
        }
        public string ID号 { get; set; }
        public string 清单编码 { get; set; }
        public string 定额编码 { get; set; }
        public string 消耗量类别 { get; set; }
        public string 消耗量编码 { get; set; }
        public string 消耗量名称 { get; set; }
        public string 规格型号 { get; set; }
        public string 消耗量单位 { get; set; }
        public decimal 数量 { get; set; }
        public decimal 定额基价 { get; set; }
        public decimal 市场价合计 { get; set; }
        public override string ToString()
        {
            return $"{ID号} - {消耗量类别} - {消耗量编码} - {消耗量名称} - {规格型号} - {消耗量单位} - {含量} - {数量} - {定额基价} - {市场价} - {市场价合计}";
        }
    }
}
