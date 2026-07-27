using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;

namespace HW_info
{
    public partial class Form5Setup : Form
    {
        public Form5Setup()
        {
            InitializeComponent();
        }

        private void Form5setup_Load(object sender, EventArgs e)
        {
            Text = "客户端配置";
            textBox1.Text = "255.255.255.255";
            checkBox1.Checked = false;
            textBox3.Text = "说明：\r\n  客户端配置信息从文件名获取，文件名中包含8位以上数字，从中提取十进制形式的IP地址，包含app字样，将收集软件列表。";
            textBox3.Text += "\r\n✔默认广播地址，可局域网内使用，跨网段需指定服务器IP地址。";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var ip = textBox1.Text;
            var ip2Int = IpToInt(ip);
            var app = checkBox1.Checked ? "-app" : "";
            var path = AppDomain.CurrentDomain.FriendlyName;
            var name = Path.GetFileNameWithoutExtension(path).Replace("-svr", "");
            var ext = Path.GetExtension(path);

            if (ip2Int > 16777216 && ip2Int < 4294967295)  //16777216 -> 1.0.0.0
            {
                textBox2.Text = $"{name}-{ip2Int}{app}{ext}";
            }
            else
            {
                textBox1.Text = "255.255.255.255";
                textBox2.Text = $"{name}{app}{ext}";
            }
        }

        public static uint IpToInt(string ip)
        {
            if (!IPAddress.TryParse(ip, out IPAddress ipaddr)) return 0;
            var bytes = ipaddr.GetAddressBytes();
            return (uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | (uint)bytes[3];
        }


        private void button2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox2.Text)) return;

            var dlg = new SaveFileDialog
            {
                InitialDirectory = Application.StartupPath,
                FileName = textBox2.Text,
                DefaultExt = ".exe",
                Filter = "程序文件|*.exe",
                OverwritePrompt = false,
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var path = Application.ExecutablePath;
                var newpath = dlg.FileName;
                WriteFiles(path, newpath);
                MessageBox.Show("保存成功!", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        void WriteFiles(string path, string newpath)
        {
            //客户端文件
            var bytes = File.ReadAllBytes(path);
            File.WriteAllBytes(newpath, bytes);
            //客户端下载HTML文件
            var html = Properties.Resources.down;
            var data = Convert.ToBase64String(bytes);
            html = html.Replace("{{data}}", $"data:application/octet-stream;base64,{data}")
                .Replace("{{file}}", Path.GetFileName(newpath));
            File.WriteAllText(Path.Combine(Application.StartupPath, "index.html"), html);
        }
    }
}
