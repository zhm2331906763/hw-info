using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HW_info
{
    public partial class Form1Cli : Form
    {

        public Form1Cli(string ip, bool app)
        {
            this.ip = ip; this.app = app;
            InitializeComponent();
            Application.ThreadException += (o, e) => MessageBox.Show(e.Exception.Message, Text);
        }

        readonly string ip;
        readonly bool app;
        readonly int port = NetSvr.Port;
        MyData data;
        string Names => textBox2.Text.Trim();
        string Addr => textBox3.Text.Trim();
        string Comment => textBox4.Text.Trim();

        private void Form1Cli_Shown(object sender, EventArgs e)
        {
            Text = "HW_info 客户端 v2.0";
            button1.Enabled = false;
            data = MyData.Get(app);
            textBox1.Text = data.ToString();
            textBox2.TextChanged += TextBox2_TextChanged;
            textBox3.TextChanged += TextBox2_TextChanged;
            textBox2.Focus();
            new ToolTip().SetToolTip(button1, ip); //提示服务端IP
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            data.Set(Names, Addr, Comment);
            var xml = data.ToXml();
            var result = NetCli.Send(xml, ip, port);
            MessageBox.Show(result ? "成功!" : "失败!", Text);
        }

        private void TextBox2_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = !string.IsNullOrWhiteSpace(Names) && !string.IsNullOrWhiteSpace(Addr);
        }
    }
}
