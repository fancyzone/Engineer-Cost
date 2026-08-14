namespace 施工定额.UI
{
    /// <summary>
    /// 管理当前选中的清单 / 定额。
    /// 由 Form1（组合根）创建并注入，不再使用全局单例。
    /// </summary>
    public class SelectionState
    {
        private string _selectedQingdanCode = "";
        private string _selectedDingeCode = "";
        private string _selectedDingeID = "";

        public string SelectedQingdanCode => _selectedQingdanCode;
        public string SelectedDingeCode => _selectedDingeCode;
        public string SelectedDingeID => _selectedDingeID;

        public void SelectQingdan(string code)
        {
            _selectedQingdanCode = code ?? "";
            _selectedDingeCode = "";
            _selectedDingeID = "";
            QingdanSelectionChanged?.Invoke(this, _selectedQingdanCode);
        }

        public void SelectDinge(string code, string id)
        {
            _selectedDingeCode = code ?? "";
            _selectedDingeID = id ?? "";
            DingeSelectionChanged?.Invoke(this, (_selectedDingeCode, _selectedDingeID));
        }

        public event EventHandler<string>? QingdanSelectionChanged;
        public event EventHandler<(string, string)>? DingeSelectionChanged;
    }
}
