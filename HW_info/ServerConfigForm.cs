using System;
using System.Windows.Forms;

namespace HW_info
{
    public partial class ServerConfigForm : Form
    {
        public ServerConfigForm()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            Hide();
            Program.RunConsoleServerWithTray();
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            ServiceManager.Install();
            ServiceManager.StartService();
            MessageBox.Show("服务已安装并设为开机自启。\n可在托盘右键菜单中启动服务。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
