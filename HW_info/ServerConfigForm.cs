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
            MessageBox.Show("服务已安装并设为开机自启。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnUninstall_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("确认卸载 HW-info 服务？\n服务停止后 Web 后台将无法访问。", "卸载服务",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                ServiceManager.Uninstall();
                MessageBox.Show("服务已卸载。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
