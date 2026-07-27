using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace HW_info
{
    public partial class Form2Svr : Form
    {
        public Form2Svr()
        {
            InitializeComponent();
            Application.ThreadException += (o, e) => MessageBox.Show(e.Exception.Message, Text);
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            //窗口标题
            Text = "HW_info 服务端 v2.0";

            //载入数据         
            var list = Data1.Datas.Select(d => d.ToString2());
            textBox1.Text = string.Join(Environment.NewLine, list);
            label2.Text = $"+{Data1.Count}";

            //启动服务
            Svr();
        }



        void Svr()
        {
            //服务端
            var svr = new NetSvr();
            svr.HttpRouteFunc["/Datas"] = req => Data1.ToHtml(Data1.Datas);
            svr.HttpRouteFunc["/Distinct"] = req => Data1.ToHtml(Data1.Distinct);
            svr.HttpRouteFunc["/Latest"] = req => Data1.ToHtml(Data1.Latest);
            svr.HttpRouteFunc["/Log"] = req =>
            {
                string resutl = null;
                Invoke(new Action(() => resutl = textBox1.Text));
                return resutl;
            };
            svr.HttpRouteFunc["/"] = req =>
            {
                var file = Path.Combine(Application.StartupPath, "index.html");
                if (!File.Exists(file)) return "没有配置客户端，找不到文件！";
                return File.ReadAllText(file);
            };

            svr.NetEvent += Svr_NetEvent;
            svr.Start();

        }

        private void Svr_NetEvent(object sender, NetSvr.NetEventArgs e)
        {
            Console.WriteLine(e.IPEnd);
            var text = Encoding.UTF8.GetString(e.Buffer);
            MyData myData = XmlConvert.Deserialize<MyData>(text) ?? JsonConvert.Deserialize<MyData>(text);
            if (myData != null)
            {
                //添加
                Data1.Add(myData);
                //更新UI
                Invoke(new Action(() =>
                {
                    label1.Text = $"收到来源：{e.IPEnd.Address} 的信息 {DateTime.Now.ToLocalTime()} ";
                    label2.Text = $"+{Data1.Count}";
                    textBox1.AppendText(myData.ToString2() + Environment.NewLine);
                }));
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            new Form4Table().ShowDialog();
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void 复制全部CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string txt = textBox1.Text;
            Clipboard.SetDataObject(txt);
        }

        private void 保存日志LToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                //桌面路径 
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                FileName = "HW_info日志.txt",
                Filter = "Text 文档|*.txt"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string txt = textBox1.Text;
                File.WriteAllText(dlg.FileName, txt);
                if (File.Exists(dlg.FileName))
                {
                    MessageBox.Show("保存成功", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void 关于AToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this, "https://gitee.com/zhux3/hw-info");
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            textBox1.Focus();//获取焦点
            textBox1.Select(textBox1.TextLength, 0);//光标定位到文本最后
            textBox1.ScrollToCaret();//滚动到光标处
        }

        private void 配置客户端OToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Form5Setup().ShowDialog();
        }

        private void hTTP服务HToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var port = NetSvr.Port;
            Help.ShowHelp(this, $"http://127.0.0.1:{port}/Datas");
        }
    }
}
