using System.ComponentModel;
using System.Reflection;

namespace 施工定额.UI
{
    public class ColumnConfig
    {
        public string FieldName { get; set; }    // 对应实体属性名
        public string HeaderText { get; set; }   // 表头显示文字
        public int Width { get; set; } = 100;
        public bool ReadOnly { get; set; } = true;
        public string Format { get; set; }       // 数字格式，如 "N2" 表示保留2位小数
        public DataGridViewContentAlignment Alignment { get; set; }
            = DataGridViewContentAlignment.MiddleLeft;
        public bool WrapMode { get; set; } = false;
    }
    public static class GridManager
    {
        // 只建列 + 绑定，用于首次初始化（传入已有的 BindingList）
        public static void BindOnce<T>(DataGridView dgv,
                                        BindingList<T> bindingList,
                                        List<ColumnConfig> columns)
        {
            dgv.SuspendLayout();
            try
            {
                dgv.AutoGenerateColumns = false;
                dgv.AllowUserToAddRows = false;
                dgv.AllowUserToDeleteRows = false;
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgv.RowHeadersVisible = false;

                typeof(DataGridView)
                    .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(dgv, true);

                if (dgv.Columns.Count == 0)
                {
                    foreach (var col in columns)
                    {
                        dgv.Columns.Add(new DataGridViewTextBoxColumn
                        {
                            Name = col.FieldName,
                            DataPropertyName = col.FieldName,
                            HeaderText = col.HeaderText,
                            Width = col.Width,
                            ReadOnly = col.ReadOnly,
                            DefaultCellStyle = new DataGridViewCellStyle
                            {
                                Format = col.Format,
                                Alignment = col.Alignment,
                                WrapMode = col.WrapMode
                                    ? DataGridViewTriState.True
                                    : DataGridViewTriState.NotSet
                            }
                        });
                    }
                }

                dgv.DataSource = bindingList; // 绑定稳定的 BindingList，不再 new
                dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            }
            finally
            {
                dgv.ResumeLayout();
            }
        }
    }


    public static class GridColumns
    {
        // 清单表的列
        public static List<ColumnConfig> Qingdan => new List<ColumnConfig>
    {
        new() { FieldName = "清单编码", HeaderText = "清单编码", Width = 120, ReadOnly = false },
        new() { FieldName = "清单名称", HeaderText = "清单名称", Width = 200, ReadOnly = false },
        new() { FieldName = "项目特征", HeaderText = "项目特征", Width = 400, ReadOnly = false, WrapMode = true },
        new() { FieldName = "单位",     HeaderText = "单位",     Width = 70 },
        new() { FieldName = "工程量",   HeaderText = "工程量",   Width = 100,
                ReadOnly = false,
                Format = "N3",
                Alignment = DataGridViewContentAlignment.MiddleRight },
        new() { FieldName = "综合单价", HeaderText = "综合单价", Width = 120,
                Format = "N2",
                Alignment = DataGridViewContentAlignment.MiddleRight },
        new() { FieldName = "综合合价", HeaderText = "综合合价", Width = 120,
                Format = "N2",
                Alignment = DataGridViewContentAlignment.MiddleRight },
    };

        // 定额表的列
        public static List<ColumnConfig> Dinge => new List<ColumnConfig>
        {
        new() { FieldName = "ID号",       HeaderText = "ID号", Width = 1, ReadOnly = true }, // 隐藏但保留
        new() { FieldName = "定额编码",   HeaderText = "定额编码",   Width = 120 },
        new() { FieldName = "定额名称",   HeaderText = "定额名称",   Width = 200 },
        new() { FieldName = "定额单位",   HeaderText = "单位",       Width = 60  },
        new() { FieldName = "定额工程量", HeaderText = "工程量",     Width = 100,
                ReadOnly = false,
                Format = "N4",
                Alignment = DataGridViewContentAlignment.MiddleRight },
        new() { FieldName = "定额单价",   HeaderText = "定额单价",   Width = 120,
                Format = "N2",
                Alignment = DataGridViewContentAlignment.MiddleRight },
        new() { FieldName = "定额合价",   HeaderText = "定额合价",   Width = 120,
                Format = "N2",
                Alignment = DataGridViewContentAlignment.MiddleRight },
    };

        // 消耗量表的列
        public static List<ColumnConfig> Xiaohaoliang => new List<ColumnConfig>
    {
        new() { FieldName = "消耗量类别", HeaderText = "类别",   Width = 60  },
        new() { FieldName = "消耗量名称", HeaderText = "名称",   Width = 200 },
        new() { FieldName = "消耗量单位", HeaderText = "单位",   Width = 60  },
        new() { FieldName = "含量",       HeaderText = "含量",   Width = 80,
                Format = "N4",
                Alignment = DataGridViewContentAlignment.MiddleRight },
        new() { FieldName = "数量",       HeaderText = "数量",   Width = 80,
                Format = "N4",
                Alignment = DataGridViewContentAlignment.MiddleRight },
        new() { FieldName = "定额基价",       HeaderText = "定额基价",   Width = 100,
                Format = "N4",
                Alignment = DataGridViewContentAlignment.MiddleRight },
        new() { FieldName = "市场价",     HeaderText = "市场价", Width = 100,
                ReadOnly = false,   // 市场价可以手动调整
                Format = "N2",
                Alignment = DataGridViewContentAlignment.MiddleRight },
        new() { FieldName = "市场价合计", HeaderText = "合计",   Width = 120,
                Format = "N2",
                Alignment = DataGridViewContentAlignment.MiddleRight },
    };
    }
}