namespace 施工定额.UI
{
    // 只管"用户选中了什么"
    public class SelectionState
    {
        private static readonly SelectionState _instance = new();
        public static SelectionState Instance => _instance;

        private string _selectedQingdanCode = "";
        private string _selectedDingeCode = "";
        private string _selectedDingeID = "";

        public string SelectedQingdanCode => _selectedQingdanCode;
        public string SelectedDingeCode => _selectedDingeCode;   // ← 检查这个
        public string SelectedDingeID => _selectedDingeID;       // ← 检查这个
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

        public event EventHandler<string> QingdanSelectionChanged;
        public event EventHandler<(string, string)> DingeSelectionChanged;
    }
}
