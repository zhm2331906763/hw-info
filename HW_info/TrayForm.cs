using System.Windows.Forms;

namespace HW_info
{
    public partial class TrayForm : Form
    {
        public TrayForm()
        {
            Text = "HW-info 托盘";
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
        }
    }
}
