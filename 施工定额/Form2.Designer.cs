namespace 施工定额
{
    partial class Form2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            comboBox1 = new ComboBox();
            treeView1 = new TreeView();
            dataGridView1 = new DataGridView();
            tabPage2 = new TabPage();
            treeView2 = new TreeView();
            comboBox2 = new ComboBox();
            dataGridView2 = new DataGridView();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView2).BeginInit();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Location = new Point(12, 12);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1376, 688);
            tabControl1.TabIndex = 0;
            tabControl1.Tag = "";
            tabControl1.SelectedIndexChanged += tabControl1_SelectedIndexChanged;
            tabControl1.MouseClick += tabControl1_MouseClick;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(comboBox1);
            tabPage1.Controls.Add(treeView1);
            tabPage1.Controls.Add(dataGridView1);
            tabPage1.Location = new Point(4, 29);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(1368, 655);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "清单库";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(6, 6);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(210, 28);
            comboBox1.TabIndex = 1;
            // 
            // treeView1
            // 
            treeView1.Location = new Point(6, 40);
            treeView1.Name = "treeView1";
            treeView1.Size = new Size(210, 610);
            treeView1.TabIndex = 1;
            treeView1.AfterSelect += treeView1_AfterSelect;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(222, 6);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.Size = new Size(1140, 644);
            dataGridView1.TabIndex = 0;
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
            dataGridView1.CellDoubleClick += dataGridView1_CellDoubleClick;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(treeView2);
            tabPage2.Controls.Add(comboBox2);
            tabPage2.Controls.Add(dataGridView2);
            tabPage2.Location = new Point(4, 29);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1368, 655);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "定额库";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // treeView2
            // 
            treeView2.Location = new Point(6, 40);
            treeView2.Name = "treeView2";
            treeView2.Size = new Size(210, 610);
            treeView2.TabIndex = 2;
            treeView2.AfterSelect += treeView2_AfterSelect;
            // 
            // comboBox2
            // 
            comboBox2.FormattingEnabled = true;
            comboBox2.Items.AddRange(new object[] { "定额_市政工程" });
            comboBox2.Location = new Point(6, 6);
            comboBox2.Name = "comboBox2";
            comboBox2.Size = new Size(210, 28);
            comboBox2.TabIndex = 1;
            // 
            // dataGridView2
            // 
            dataGridView2.AllowUserToAddRows = false;
            dataGridView2.AllowUserToDeleteRows = false;
            dataGridView2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView2.Location = new Point(222, 6);
            dataGridView2.Name = "dataGridView2";
            dataGridView2.ReadOnly = true;
            dataGridView2.RowHeadersWidth = 51;
            dataGridView2.Size = new Size(1140, 644);
            dataGridView2.TabIndex = 0;
            dataGridView2.CellDoubleClick += dataGridView2_CellDoubleClick;
            // 
            // Form2
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1394, 705);
            Controls.Add(tabControl1);
            Name = "Form2";
            Text = "Form2";
            Load += Form2_Load;
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView2).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControl1;
        private TabPage tabPage1;
        private DataGridView dataGridView1;
        private TabPage tabPage2;
        private TreeView treeView1;
        private ComboBox comboBox1;
        private DataGridView dataGridView2;
        private ComboBox comboBox2;
        private TreeView treeView2;
    }
}