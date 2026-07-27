namespace HW_info
{
    partial class Form4Table
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.contextMenuStrip2 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.详细信息IToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.导出结果OToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.查看全部ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.去重ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.最新提交数据ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.contextMenuStrip2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // contextMenuStrip2
            // 
            this.contextMenuStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.详细信息IToolStripMenuItem,
            this.导出结果OToolStripMenuItem,
            this.toolStripSeparator1,
            this.查看全部ToolStripMenuItem,
            this.去重ToolStripMenuItem,
            this.最新提交数据ToolStripMenuItem});
            this.contextMenuStrip2.Name = "contextMenuStrip2";
            this.contextMenuStrip2.Size = new System.Drawing.Size(176, 120);
            // 
            // 详细信息IToolStripMenuItem
            // 
            this.详细信息IToolStripMenuItem.Name = "详细信息IToolStripMenuItem";
            this.详细信息IToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.详细信息IToolStripMenuItem.Text = "详细信息(&I)";
            this.详细信息IToolStripMenuItem.Click += new System.EventHandler(this.DataGridView1_CellContentDoubleClick);
            // 
            // 导出结果OToolStripMenuItem
            // 
            this.导出结果OToolStripMenuItem.Name = "导出结果OToolStripMenuItem";
            this.导出结果OToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.导出结果OToolStripMenuItem.Text = "导出为CSV表格(&V)";
            this.导出结果OToolStripMenuItem.Click += new System.EventHandler(this.导出OToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(172, 6);
            // 
            // 查看全部ToolStripMenuItem
            // 
            this.查看全部ToolStripMenuItem.Name = "查看全部ToolStripMenuItem";
            this.查看全部ToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.查看全部ToolStripMenuItem.Text = "查看全部";
            this.查看全部ToolStripMenuItem.Click += new System.EventHandler(this.查看全部ToolStripMenuItem_Click);
            // 
            // 去重ToolStripMenuItem
            // 
            this.去重ToolStripMenuItem.Name = "去重ToolStripMenuItem";
            this.去重ToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.去重ToolStripMenuItem.Text = "去重数据";
            this.去重ToolStripMenuItem.Click += new System.EventHandler(this.去重ToolStripMenuItem_Click);
            // 
            // 最新提交数据ToolStripMenuItem
            // 
            this.最新提交数据ToolStripMenuItem.Name = "最新提交数据ToolStripMenuItem";
            this.最新提交数据ToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.最新提交数据ToolStripMenuItem.Text = "最新数据";
            this.最新提交数据ToolStripMenuItem.Click += new System.EventHandler(this.最新提交数据ToolStripMenuItem_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.Gainsboro;
            this.dataGridView1.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dataGridView1.BackgroundColor = System.Drawing.SystemColors.ControlLight;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.ContextMenuStrip = this.contextMenuStrip2;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowTemplate.Height = 23;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(704, 484);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(this.dataGridView1_RowPostPaint);
            // 
            // Form4Table
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(704, 484);
            this.Controls.Add(this.dataGridView1);
            this.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "Form4Table";
            this.ShowInTaskbar = false;
            this.Text = "Form4";
            this.Load += new System.EventHandler(this.Form4_Load);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Form4_KeyPress);
            this.contextMenuStrip2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip2;
        private System.Windows.Forms.ToolStripMenuItem 导出结果OToolStripMenuItem;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.ToolStripMenuItem 详细信息IToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem 查看全部ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 去重ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 最新提交数据ToolStripMenuItem;
    }
}