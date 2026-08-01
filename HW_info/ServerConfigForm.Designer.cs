namespace HW_info
{
    partial class ServerConfigForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnInstall = new System.Windows.Forms.Button();
            this.btnUninstall = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.lblDesc = new System.Windows.Forms.Label();
            this.SuspendLayout();

            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("微软雅黑", 18F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(24, 20);
            this.lblTitle.Text = "HW-info 硬件资产管理系统";

            this.lblDesc.AutoSize = true;
            this.lblDesc.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.lblDesc.ForeColor = System.Drawing.Color.Gray;
            this.lblDesc.Location = new System.Drawing.Point(26, 58);
            this.lblDesc.Text = "启动服务端收集局域网内电脑硬件信息，通过 Web 管理后台查看。";

            this.btnStart.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.btnStart.Location = new System.Drawing.Point(28, 100);
            this.btnStart.Size = new System.Drawing.Size(200, 50);
            this.btnStart.Text = "▶ 启动服务器";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);

            this.btnInstall.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.btnInstall.Location = new System.Drawing.Point(240, 100);
            this.btnInstall.Size = new System.Drawing.Size(200, 50);
            this.btnInstall.Text = "⏺ 安装服务(开机自启)";
            this.btnInstall.UseVisualStyleBackColor = true;
            this.btnInstall.Click += new System.EventHandler(this.btnInstall_Click);

            this.btnUninstall.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.btnUninstall.Location = new System.Drawing.Point(28, 162);
            this.btnUninstall.Size = new System.Drawing.Size(200, 30);
            this.btnUninstall.Text = "✕ 卸载服务";
            this.btnUninstall.UseVisualStyleBackColor = true;
            this.btnUninstall.Click += new System.EventHandler(this.btnUninstall_Click);

            this.btnExit.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.btnExit.Location = new System.Drawing.Point(375, 162);
            this.btnExit.Size = new System.Drawing.Size(65, 30);
            this.btnExit.Text = "退出";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 208);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblDesc);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnInstall);
            this.Controls.Add(this.btnUninstall);
            this.Controls.Add(this.btnExit);
            this.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "HW-info 硬件资产管理系统";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblDesc;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnInstall;
        private System.Windows.Forms.Button btnUninstall;
        private System.Windows.Forms.Button btnExit;
    }
}
