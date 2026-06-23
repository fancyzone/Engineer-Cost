namespace 施工定额
{
    partial class Form1
    {
        
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            TreeNode treeNode1 = new TreeNode("人");
            TreeNode treeNode2 = new TreeNode("材");
            TreeNode treeNode3 = new TreeNode("机");
            TreeNode treeNode4 = new TreeNode("根节点", new TreeNode[] { treeNode1, treeNode2, treeNode3 });
            tabControl2 = new TabControl();
            tabPage3 = new TabPage();
            dataGridView2 = new DataGridView();
            tabPage4 = new TabPage();
            tabCostSummary = new TabPage();
            dataGridView4 = new DataGridView();
            tabRenCaiJi = new TabPage();
            treeView1 = new TreeView();
            dataGridView3 = new DataGridView();
            tabPage7 = new TabPage();
            tabPage2 = new TabPage();
            tabPage1 = new TabPage();
            DataGridView_dinge = new DataGridView();
            dataGridView1 = new DataGridView();
            tabControl1 = new TabControl();
            tabControl2.SuspendLayout();
            tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView2).BeginInit();
            tabCostSummary.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView4).BeginInit();
            tabRenCaiJi.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView3).BeginInit();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)DataGridView_dinge).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            tabControl1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl2
            // 
            tabControl2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tabControl2.Controls.Add(tabPage3);
            tabControl2.Controls.Add(tabPage4);
            tabControl2.Location = new Point(117, 724);
            tabControl2.Name = "tabControl2";
            tabControl2.SelectedIndex = 0;
            tabControl2.Size = new Size(1782, 411);
            tabControl2.TabIndex = 1;
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(dataGridView2);
            tabPage3.Location = new Point(4, 29);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(1774, 378);
            tabPage3.TabIndex = 0;
            tabPage3.Text = "工料机";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // dataGridView2
            // 
            dataGridView2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView2.Location = new Point(6, 6);
            dataGridView2.Name = "dataGridView2";
            dataGridView2.RowHeadersWidth = 51;
            dataGridView2.Size = new Size(1756, 366);
            dataGridView2.TabIndex = 0;
            dataGridView2.CellValueChanged += dataGridView2_CellValueChanged;
            // 
            // tabPage4
            // 
            tabPage4.Location = new Point(4, 29);
            tabPage4.Name = "tabPage4";
            tabPage4.Padding = new Padding(3);
            tabPage4.Size = new Size(1774, 378);
            tabPage4.TabIndex = 1;
            tabPage4.Text = "tabPage4";
            tabPage4.UseVisualStyleBackColor = true;
            // 
            // tabCostSummary
            // 
            tabCostSummary.Controls.Add(dataGridView4);
            tabCostSummary.Location = new Point(4, 29);
            tabCostSummary.Name = "tabCostSummary";
            tabCostSummary.Padding = new Padding(3);
            tabCostSummary.Size = new Size(1774, 657);
            tabCostSummary.TabIndex = 3;
            tabCostSummary.Text = "费用汇总";
            tabCostSummary.UseVisualStyleBackColor = true;
            // 
            // dataGridView4
            // 
            dataGridView4.AllowUserToAddRows = false;
            dataGridView4.AllowUserToDeleteRows = false;
            dataGridView4.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView4.Location = new Point(10, 6);
            dataGridView4.Name = "dataGridView4";
            dataGridView4.ReadOnly = true;
            dataGridView4.RowHeadersWidth = 51;
            dataGridView4.Size = new Size(1067, 334);
            dataGridView4.TabIndex = 0;
            // 
            // tabRenCaiJi
            // 
            tabRenCaiJi.Controls.Add(treeView1);
            tabRenCaiJi.Controls.Add(dataGridView3);
            tabRenCaiJi.Location = new Point(4, 29);
            tabRenCaiJi.Name = "tabRenCaiJi";
            tabRenCaiJi.Padding = new Padding(3);
            tabRenCaiJi.Size = new Size(1774, 657);
            tabRenCaiJi.TabIndex = 2;
            tabRenCaiJi.Text = "人才机汇总";
            tabRenCaiJi.UseVisualStyleBackColor = true;
            // 
            // treeView1
            // 
            treeView1.Location = new Point(4, 6);
            treeView1.Name = "treeView1";
            treeNode1.Name = "人";
            treeNode1.Text = "人";
            treeNode2.Name = "材";
            treeNode2.Text = "材";
            treeNode3.Name = "机";
            treeNode3.Text = "机";
            treeNode4.Name = "根节点";
            treeNode4.Text = "根节点";
            treeView1.Nodes.AddRange(new TreeNode[] { treeNode4 });
            treeView1.Size = new Size(164, 449);
            treeView1.TabIndex = 1;
            treeView1.AfterSelect += treeView1_AfterSelect;
            // 
            // dataGridView3
            // 
            dataGridView3.AllowUserToAddRows = false;
            dataGridView3.AllowUserToDeleteRows = false;
            dataGridView3.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView3.Location = new Point(174, 6);
            dataGridView3.Name = "dataGridView3";
            dataGridView3.ReadOnly = true;
            dataGridView3.RowHeadersWidth = 51;
            dataGridView3.Size = new Size(1588, 449);
            dataGridView3.TabIndex = 0;
            // 
            // tabPage7
            // 
            tabPage7.Location = new Point(4, 29);
            tabPage7.Name = "tabPage7";
            tabPage7.Padding = new Padding(3);
            tabPage7.Size = new Size(1774, 657);
            tabPage7.TabIndex = 4;
            tabPage7.Text = "其他";
            tabPage7.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            tabPage2.Location = new Point(4, 29);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1774, 657);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "措施";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(DataGridView_dinge);
            tabPage1.Controls.Add(dataGridView1);
            tabPage1.Location = new Point(4, 29);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(1774, 657);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "分部分项";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // DataGridView_dinge
            // 
            DataGridView_dinge.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            DataGridView_dinge.Location = new Point(10, 473);
            DataGridView_dinge.Name = "DataGridView_dinge";
            DataGridView_dinge.RowHeadersWidth = 51;
            DataGridView_dinge.Size = new Size(1752, 170);
            DataGridView_dinge.TabIndex = 0;
            DataGridView_dinge.CellClick += DataGridView_dinge_CellClick;
            DataGridView_dinge.CellValueChanged += DataGridView_dinge_CellValueChanged;
            // 
            // dataGridView1
            // 
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(10, 6);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.Size = new Size(1752, 452);
            dataGridView1.TabIndex = 0;
            dataGridView1.CellClick += dataGridView1_CellClick;
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
            dataGridView1.CellDoubleClick += dataGridView1_CellDoubleClick;
            dataGridView1.CellEndEdit += dataGridView1_CellEndEdit;
            dataGridView1.CellValueChanged += dataGridView1_CellValueChanged;
            // 
            // tabControl1
            // 
            tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage7);
            tabControl1.Controls.Add(tabRenCaiJi);
            tabControl1.Controls.Add(tabCostSummary);
            tabControl1.Location = new Point(117, 28);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1782, 690);
            tabControl1.TabIndex = 0;
            tabControl1.SelectedIndexChanged += tabControl1_SelectedIndexChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1971, 1147);
            Controls.Add(tabControl2);
            Controls.Add(tabControl1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            tabControl2.ResumeLayout(false);
            tabPage3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView2).EndInit();
            tabCostSummary.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView4).EndInit();
            tabRenCaiJi.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView3).EndInit();
            tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)DataGridView_dinge).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            tabControl1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private TabControl tabControl2;
        private TabPage tabPage3;
        private TabPage tabPage4;
        private DataGridView dataGridView2;
        private TabPage tabCostSummary;
        private DataGridView dataGridView4;
        private TabPage tabRenCaiJi;
        private TreeView treeView1;
        private DataGridView dataGridView3;
        private TabPage tabPage7;
        private TabPage tabPage2;
        private TabPage tabPage1;
        private DataGridView DataGridView_dinge;
        private DataGridView dataGridView1;
        private TabControl tabControl1;
    }
}
