namespace 施工定额.Service
{
    /// <summary>
    /// 从系统定额库导入清单/定额到用户工程库的抽象。
    /// </summary>
    public interface IImportService
    {
        /// <summary>
        /// 从系统库导入一条清单（连同它下属的所有定额和消耗量）到用户库。
        /// </summary>
        void ImportQingdan(string sysQingdanCode, string name, string feature, string unit);

        /// <summary>
        /// 从系统库导入单条定额（连同它的消耗量）到用户库的指定清单下。
        /// </summary>
        void ImportDinge(string targetQingdanCode, string sysId,
            string dingeCode, string name, string unit);
    }
}
