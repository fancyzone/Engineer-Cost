namespace 施工定额
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            TreeNode treeNode1 = new TreeNode("人");
            TreeNode treeNode2 = new TreeNode("材");
            TreeNode treeNode3 = new TreeNode("机");
            TreeNode treeNode4 = new TreeNode("根节点", new TreeNode[] { treeNode1, treeNode2, treeNode3 });
            TreeNode treeNodeProject = new TreeNode("工程结构（待接入）");
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));

            mainSplit = new SplitContainer();
            treeProject = new TreeView();
            rightSplit = new SplitContainer();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            qingdanSplit = new SplitContainer();
            dataGridView1 = new DataGridView();
            DataGridView_dinge = new DataGridView();
            tabRenCaiJi = new TabPage();
            treeView1 = new TreeView();
            dataGridView3 = new DataGridView();
            tabCostSummary = new TabPage();
            dataGridView4 = new DataGridView();
            tabControl2 = new TabControl();
            tabPage3 = new TabPage();
            dataGridView2 = new DataGridView();
            menuStrip1 = new MenuStrip();
            文件ToolStripMenuItem = new ToolStripMenuItem();
            打开ToolStripMenuItem = new ToolStripMenuItem();
            保存ToolStripMenuItem = new ToolStripMenuItem();
            toolStrip1 = new ToolStrip();
            toolStripButton1 = new ToolStripButton();
            toolStripButton2 = new ToolStripButton();

            ((System.ComponentModel.ISupportInitialize)mainSplit).BeginInit();
            mainSplit.Panel1.SuspendLayout();
            mainSplit.Panel2.SuspendLayout();
            mainSplit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)rightSplit).BeginInit();
            rightSplit.Panel1.SuspendLayout();
            rightSplit.Panel2.SuspendLayout();
            rightSplit.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)qingdanSplit).BeginInit();
            qingdanSplit.Panel1.SuspendLayout();
            qingdanSplit.Panel2.SuspendLayout();
            qingdanSplit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)DataGridView_dinge).BeginInit();
            tabRenCaiJi.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView3).BeginInit();
            tabCostSummary.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView4).BeginInit();
            tabControl2.SuspendLayout();
            tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView2).BeginInit();
            menuStrip1.SuspendLayout();
            toolStrip1.SuspendLayout();
            SuspendLayout();

            //
            // mainSplit — 左工程树 | 右内容
            // 注意：初始化时不要设 Panel*MinSize / SplitterDistance，
            // 控件尚未有最终尺寸时会抛「SplitterDistance 必须在 … 之间」。
            // 比例在 Form1.Layout.ApplySplittersAfterLayout 里设置。
            //
            mainSplit.Dock = DockStyle.Fill;
            mainSplit.FixedPanel = FixedPanel.Panel1;
            mainSplit.Name = "mainSplit";
            mainSplit.Orientation = Orientation.Vertical;
            mainSplit.Panel1MinSize = 0;
            mainSplit.Panel2MinSize = 0;
            mainSplit.SplitterWidth = 5;
            mainSplit.TabIndex = 0;
            mainSplit.Panel1.Controls.Add(treeProject);
            mainSplit.Panel2.Controls.Add(rightSplit);

            //
            // treeProject
            //
            treeProject.Dock = DockStyle.Fill;
            treeProject.HideSelection = false;
            treeProject.Name = "treeProject";
            treeNodeProject.Name = "placeholder";
            treeNodeProject.Text = "工程结构（待接入）";
            treeProject.Nodes.AddRange(new TreeNode[] { treeNodeProject });
            treeProject.TabIndex = 0;

            //
            // rightSplit — 上主 Tab | 下工料机
            //
            rightSplit.Dock = DockStyle.Fill;
            rightSplit.Name = "rightSplit";
            rightSplit.Orientation = Orientation.Horizontal;
            rightSplit.Panel1MinSize = 0;
            rightSplit.Panel2MinSize = 0;
            rightSplit.SplitterWidth = 5;
            rightSplit.TabIndex = 0;
            rightSplit.Panel1.Controls.Add(tabControl1);
            rightSplit.Panel2.Controls.Add(tabControl2);

            //
            // tabControl1
            //
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabRenCaiJi);
            tabControl1.Controls.Add(tabCostSummary);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.TabIndex = 0;
            tabControl1.SelectedIndexChanged += tabControl1_SelectedIndexChanged;

            //
            // tabPage1 分部分项
            //
            tabPage1.Controls.Add(qingdanSplit);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "分部分项";
            tabPage1.UseVisualStyleBackColor = true;

            //
            // qingdanSplit — 上清单 | 下定额
            //
            qingdanSplit.Dock = DockStyle.Fill;
            qingdanSplit.Name = "qingdanSplit";
            qingdanSplit.Orientation = Orientation.Horizontal;
            qingdanSplit.Panel1MinSize = 0;
            qingdanSplit.Panel2MinSize = 0;
            qingdanSplit.SplitterWidth = 5;
            qingdanSplit.TabIndex = 0;
            qingdanSplit.Panel1.Controls.Add(dataGridView1);
            qingdanSplit.Panel2.Controls.Add(DataGridView_dinge);

            //
            // dataGridView1 清单
            //
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.TabIndex = 0;
            dataGridView1.CellClick += dataGridView1_CellClick;
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
            dataGridView1.CellDoubleClick += dataGridView1_CellDoubleClick;
            dataGridView1.CellEndEdit += dataGridView1_CellEndEdit;
            dataGridView1.CellValueChanged += dataGridView1_CellValueChanged;

            //
            // DataGridView_dinge 定额
            //
            DataGridView_dinge.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            DataGridView_dinge.Dock = DockStyle.Fill;
            DataGridView_dinge.Name = "DataGridView_dinge";
            DataGridView_dinge.RowHeadersWidth = 51;
            DataGridView_dinge.TabIndex = 0;
            DataGridView_dinge.CellClick += DataGridView_dinge_CellClick;
            DataGridView_dinge.CellValueChanged += DataGridView_dinge_CellValueChanged;

            //
            // tabRenCaiJi 人材机汇总
            //
            tabRenCaiJi.Controls.Add(dataGridView3);
            tabRenCaiJi.Controls.Add(treeView1);
            tabRenCaiJi.Name = "tabRenCaiJi";
            tabRenCaiJi.Padding = new Padding(3);
            tabRenCaiJi.TabIndex = 1;
            tabRenCaiJi.Text = "人材机汇总";
            tabRenCaiJi.UseVisualStyleBackColor = true;

            //
            // treeView1
            //
            treeView1.Dock = DockStyle.Left;
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
            treeView1.TabIndex = 1;
            treeView1.Width = 160;
            treeView1.AfterSelect += treeView1_AfterSelect;

            //
            // dataGridView3
            //
            dataGridView3.AllowUserToAddRows = false;
            dataGridView3.AllowUserToDeleteRows = false;
            dataGridView3.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView3.Dock = DockStyle.Fill;
            dataGridView3.Name = "dataGridView3";
            dataGridView3.ReadOnly = true;
            dataGridView3.RowHeadersWidth = 51;
            dataGridView3.TabIndex = 0;

            //
            // tabCostSummary 费用汇总
            //
            tabCostSummary.Controls.Add(dataGridView4);
            tabCostSummary.Name = "tabCostSummary";
            tabCostSummary.Padding = new Padding(3);
            tabCostSummary.TabIndex = 2;
            tabCostSummary.Text = "费用汇总";
            tabCostSummary.UseVisualStyleBackColor = true;

            //
            // dataGridView4
            //
            dataGridView4.AllowUserToAddRows = false;
            dataGridView4.AllowUserToDeleteRows = false;
            dataGridView4.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView4.Dock = DockStyle.Fill;
            dataGridView4.Name = "dataGridView4";
            dataGridView4.ReadOnly = true;
            dataGridView4.RowHeadersWidth = 51;
            dataGridView4.TabIndex = 0;

            //
            // tabControl2 工料机
            //
            tabControl2.Controls.Add(tabPage3);
            tabControl2.Dock = DockStyle.Fill;
            tabControl2.Name = "tabControl2";
            tabControl2.SelectedIndex = 0;
            tabControl2.TabIndex = 1;

            //
            // tabPage3
            //
            tabPage3.Controls.Add(dataGridView2);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.TabIndex = 0;
            tabPage3.Text = "工料机";
            tabPage3.UseVisualStyleBackColor = true;

            //
            // dataGridView2
            //
            dataGridView2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView2.Dock = DockStyle.Fill;
            dataGridView2.Name = "dataGridView2";
            dataGridView2.RowHeadersWidth = 51;
            dataGridView2.TabIndex = 0;
            dataGridView2.CellValueChanged += dataGridView2_CellValueChanged;

            //
            // menuStrip1
            //
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { 文件ToolStripMenuItem });
            menuStrip1.Name = "menuStrip1";
            menuStrip1.TabIndex = 3;
            menuStrip1.Text = "menuStrip1";

            //
            // 文件ToolStripMenuItem
            //
            文件ToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { 打开ToolStripMenuItem, 保存ToolStripMenuItem });
            文件ToolStripMenuItem.Name = "文件ToolStripMenuItem";
            文件ToolStripMenuItem.Size = new Size(53, 24);
            文件ToolStripMenuItem.Text = "文件";

            //
            // 打开ToolStripMenuItem
            //
            打开ToolStripMenuItem.Name = "打开ToolStripMenuItem";
            打开ToolStripMenuItem.Size = new Size(122, 26);
            打开ToolStripMenuItem.Text = "打开";

            //
            // 保存ToolStripMenuItem
            //
            保存ToolStripMenuItem.Name = "保存ToolStripMenuItem";
            保存ToolStripMenuItem.Size = new Size(122, 26);
            保存ToolStripMenuItem.Text = "保存";

            //
            // toolStrip1
            //
            toolStrip1.ImageScalingSize = new Size(20, 20);
            toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripButton1, toolStripButton2 });
            toolStrip1.Name = "toolStrip1";
            toolStrip1.TabIndex = 4;
            toolStrip1.Text = "toolStrip1";

            //
            // toolStripButton1
            //
            toolStripButton1.Image = (Image)resources.GetObject("toolStripButton1.Image");
            toolStripButton1.ImageTransparentColor = Color.Magenta;
            toolStripButton1.Name = "toolStripButton1";
            toolStripButton1.Size = new Size(73, 44);
            toolStripButton1.Text = "检查更新";
            toolStripButton1.TextImageRelation = TextImageRelation.ImageAboveText;
            toolStripButton1.Click += toolStripButton1_Click;

            //
            // toolStripButton2
            //
            toolStripButton2.Image = (Image)resources.GetObject("toolStripButton2.Image");
            toolStripButton2.ImageTransparentColor = Color.Magenta;
            toolStripButton2.Name = "toolStripButton2";
            toolStripButton2.Size = new Size(73, 44);
            toolStripButton2.Text = "导出";
            toolStripButton2.TextImageRelation = TextImageRelation.ImageAboveText;
            toolStripButton2.Click += toolStripButton2_Click;

            //
            // Form1 — 控件添加顺序：内容 → 工具栏 → 菜单（菜单最顶）
            //
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1400, 900);
            Controls.Add(mainSplit);
            Controls.Add(toolStrip1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "施工定额";
            Load += Form1_Load;

            mainSplit.Panel1.ResumeLayout(false);
            mainSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)mainSplit).EndInit();
            mainSplit.ResumeLayout(false);
            rightSplit.Panel1.ResumeLayout(false);
            rightSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)rightSplit).EndInit();
            rightSplit.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            qingdanSplit.Panel1.ResumeLayout(false);
            qingdanSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)qingdanSplit).EndInit();
            qingdanSplit.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)DataGridView_dinge).EndInit();
            tabRenCaiJi.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView3).EndInit();
            tabCostSummary.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView4).EndInit();
            tabControl2.ResumeLayout(false);
            tabPage3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView2).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private SplitContainer mainSplit;
        private TreeView treeProject;
        private SplitContainer rightSplit;
        private SplitContainer qingdanSplit;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private DataGridView dataGridView1;
        private DataGridView DataGridView_dinge;
        private TabPage tabRenCaiJi;
        private TreeView treeView1;
        private DataGridView dataGridView3;
        private TabPage tabCostSummary;
        private DataGridView dataGridView4;
        private TabControl tabControl2;
        private TabPage tabPage3;
        private DataGridView dataGridView2;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem 文件ToolStripMenuItem;
        private ToolStripMenuItem 打开ToolStripMenuItem;
        private ToolStripMenuItem 保存ToolStripMenuItem;
        private ToolStrip toolStrip1;
        private ToolStripButton toolStripButton1;
        private ToolStripButton toolStripButton2;
    }
}
