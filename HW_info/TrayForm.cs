using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.ServiceProcess;
using Microsoft.Win32;

namespace HW_info
{
    public partial class TrayForm : Form
    {
        private System.Threading.Timer _statusTimer;

        public TrayForm()
        {
            InitializeComponent();
            Text = "HW-info 托盘";
            notifyIcon.Icon = System.Drawing.SystemIcons.Application;
        }

        private void TrayForm_Load(object sender, EventArgs e)
        {
            RefreshServiceStatus();
            _statusTimer = new System.Threading.Timer(_ => RefreshServiceStatus(), null, 5000, 5000);
        }

        private void RefreshServiceStatus()
        {
            try
            {
                var installed = ServiceManager.IsInstalled();
                var status = ServiceManager.GetStatus();

                if (!installed)
                {
                    miServiceStatus.Text = "服务状态: 未安装";
                    miStartService.Enabled = false;
                    miStopService.Enabled = false;
                    miRestartService.Enabled = false;
                }
                else if (status == ServiceControllerStatus.Running)
                {
                    miServiceStatus.Text = "服务状态: 运行中";
                    miStartService.Enabled = false;
                    miStopService.Enabled = true;
                    miRestartService.Enabled = true;
                }
                else
                {
                    miServiceStatus.Text = "服务状态: 已停止";
                    miStartService.Enabled = true;
                    miStopService.Enabled = false;
                    miRestartService.Enabled = false;
                }
            }
            catch { }
        }

        private bool IsAutoStartEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                    return key?.GetValue("HWInfoTray") != null;
            }
            catch { return false; }
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (enable)
                        key?.SetValue("HWInfoTray", Application.ExecutablePath + " -tray");
                    else
                        key?.DeleteValue("HWInfoTray", false);
                }
            }
            catch { }
        }

        private void MiOpenWeb_Click(object sender, EventArgs e)
        {
            Process.Start($"http://127.0.0.1:{NetSvr.Port}/");
        }

        private void MiStartService_Click(object sender, EventArgs e)
        {
            if (!ServiceManager.IsInstalled())
            {
                var result = MessageBox.Show("服务尚未安装，是否现在安装？", "安装服务", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    ServiceManager.Install();
                    ServiceManager.StartService();
                }
            }
            else
            {
                ServiceManager.StartService();
            }
            Thread.Sleep(1000);
            RefreshServiceStatus();
        }

        private void MiStopService_Click(object sender, EventArgs e)
        {
            ServiceManager.StopService();
            Thread.Sleep(1000);
            RefreshServiceStatus();
        }

        private void MiRestartService_Click(object sender, EventArgs e)
        {
            ServiceManager.StopService();
            Thread.Sleep(2000);
            ServiceManager.StartService();
            Thread.Sleep(1000);
            RefreshServiceStatus();
        }

        private void MiAutoStart_Click(object sender, EventArgs e)
        {
            var enable = !IsAutoStartEnabled();
            SetAutoStart(enable);
            miAutoStart.Checked = enable;
        }

        private void MiHelp_Click(object sender, EventArgs e)
        {
            var msg = "HW-info 硬件资产管理系统 v2.0\r\n\r\n"
                + "【服务端启动】\r\n"
                + "  HW-info-svr.exe -svr          控制台+托盘模式\r\n"
                + "  HW-info-svr.exe -install      安装Windows服务(开机自启)\r\n"
                + "  HW-info-svr.exe -uninstall    卸载服务\r\n\r\n"
                + "【客户端部署】\r\n"
                + "  将 exe 重命名: HW-info-<IP十进制数字>.exe\r\n"
                + "  例: 服务器 192.168.1.100 → 3232235876\r\n"
                + "  HW-info-3232235876.exe\r\n\r\n"
                + "  或用命令行:\r\n"
                + "  HW-info-svr.exe --ip 192.168.1.100\r\n\r\n"
                + "【管理后台】\r\n"
                + "  http://本机IP:51528\r\n\r\n"
                + "【静默提交(脚本用)】\r\n"
                + "  --ip <IP> --name <姓名> --addr <位置> [--desc <备注>]\r\n\r\n"
                + "【数据文件】\r\n"
                + "  HW_info.db 与 exe 同目录，备份此文件即可";
            MessageBox.Show(msg, "使用说明", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MiExit_Click(object sender, EventArgs e)
        {
            _statusTimer?.Dispose();
            notifyIcon.Visible = false;
            Application.Exit();
        }
    }
}
