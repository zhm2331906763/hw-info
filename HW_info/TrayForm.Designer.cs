namespace HW_info
{
    partial class TrayForm
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
            this.components = new System.ComponentModel.Container();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.miOpenWeb = new System.Windows.Forms.ToolStripMenuItem();
            this.miSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.miServiceStatus = new System.Windows.Forms.ToolStripMenuItem();
            this.miStartService = new System.Windows.Forms.ToolStripMenuItem();
            this.miStopService = new System.Windows.Forms.ToolStripMenuItem();
            this.miRestartService = new System.Windows.Forms.ToolStripMenuItem();
            this.miSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.miAutoStart = new System.Windows.Forms.ToolStripMenuItem();
            this.miSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.miExit = new System.Windows.Forms.ToolStripMenuItem();

            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.miOpenWeb, this.miSeparator1, this.miServiceStatus,
                this.miStartService, this.miStopService, this.miRestartService,
                this.miSeparator2, this.miAutoStart, this.miSeparator3, this.miExit,
            });

            this.miOpenWeb.Text = "打开 Web 管理界面(&W)";
            this.miServiceStatus.Text = "服务状态: 未知";
            this.miServiceStatus.Enabled = false;
            this.miStartService.Text = "启动服务(&S)";
            this.miStopService.Text = "停止服务(&T)";
            this.miRestartService.Text = "重启服务(&R)";
            this.miAutoStart.Text = "开机自启动";
            this.miExit.Text = "退出(&X)";

            this.notifyIcon.ContextMenuStrip = this.contextMenu;
            this.notifyIcon.Text = "HW-info 资产管理";
            this.notifyIcon.Visible = true;

            this.miOpenWeb.Click += MiOpenWeb_Click;
            this.miStartService.Click += MiStartService_Click;
            this.miStopService.Click += MiStopService_Click;
            this.miRestartService.Click += MiRestartService_Click;
            this.miAutoStart.Click += MiAutoStart_Click;
            this.miExit.Click += MiExit_Click;

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(0, 0);
            this.ControlBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.Load += TrayForm_Load;
        }

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem miOpenWeb;
        private System.Windows.Forms.ToolStripSeparator miSeparator1;
        private System.Windows.Forms.ToolStripMenuItem miServiceStatus;
        private System.Windows.Forms.ToolStripMenuItem miStartService;
        private System.Windows.Forms.ToolStripMenuItem miStopService;
        private System.Windows.Forms.ToolStripMenuItem miRestartService;
        private System.Windows.Forms.ToolStripSeparator miSeparator2;
        private System.Windows.Forms.ToolStripMenuItem miAutoStart;
        private System.Windows.Forms.ToolStripSeparator miSeparator3;
        private System.Windows.Forms.ToolStripMenuItem miExit;
    }
}
