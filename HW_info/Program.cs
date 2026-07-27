using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HW_info
{
    static class Program
    {

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //根据参数运行
            var action = args.Length > 0 ? ParseArguments() : ParseFileName();
            action.Invoke();
        }


        /// <summary>
        /// 解析文件名
        /// </summary>
        static Action ParseFileName()
        {
            //程序文件名
            var fileName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);

            if (Regex.IsMatch(fileName, "-svr", RegexOptions.IgnoreCase))
            {
                //返回服务端
                return new Action(() => Application.Run(new Form2Svr()));
            }
            else
            {
                //是否获取APP列表
                var app = Regex.IsMatch(fileName, "app", RegexOptions.IgnoreCase);
                //默认IP
                var ip = IPAddress.Broadcast.ToString();

                //取十进制IP
                var match = Regex.Match(fileName, @"\d{8,}");
                if (match.Success && IPAddress.TryParse(match.Value, out IPAddress iPAddress))
                {
                    ip = iPAddress.ToString();
                }

                //返回客户端
                return new Action(() => Application.Run(new Form1Cli(ip, app)));
            }
        }


        /// <summary>
        /// 解析命令行参数
        /// </summary>
        /// <param name="args"></param>
        static Action ParseArguments()
        {
            //命令行参数
            var args = Environment.GetCommandLineArgs();

            var parameters = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--") && i + 1 < args.Length)
                {
                    parameters[args[i].Substring(2)] = args[i + 1];
                    i++; // 跳过下一个参数（值）
                }
                else if (args[i].StartsWith("-"))
                {
                    parameters[args[i].Substring(1)] = "true"; // 标记为存在
                }
            }

            if (parameters.TryGetValue("?", out _) || parameters.TryGetValue("h", out _))
            {
                //启动帮助
                return Help;
            }


            if (parameters.TryGetValue("svr", out _))
            {
                //返回服务端
                return new Action(() => Application.Run(new Form2Svr()));
            }

            //是否获取APP列表
            var app = parameters.TryGetValue("app", out _); 

            //默认IP
            var ip = IPAddress.Broadcast.ToString();
            if (parameters.TryGetValue("ip", out string ip1) && IPAddress.TryParse(ip1, out IPAddress address))
            {
                ip = address.ToString();
            }

            //解析姓名，位置
            if (parameters.TryGetValue("name", out string name) && parameters.TryGetValue("addr", out string addr))
            {
                //备注信息
                parameters.TryGetValue("desc", out string desc);
                //静默发送
                return new Action(() => Send(app, ip, name, addr, desc));
            }

            //返回客户端
            return new Action(() => Application.Run(new Form1Cli(ip, app)));

        }

        private static void Help()
        {
            //帮助信息
            var appName = AppDomain.CurrentDomain.FriendlyName;
            var sb = new StringBuilder();
            sb.AppendLine($"启动服务端:\r\n {appName} -svr");
            sb.AppendLine();
            sb.AppendLine($"启动客户端，指定IP，收集应用程序列表:\r\n {appName} --ip <IP> [-app]");
            sb.AppendLine();
            sb.AppendLine($"启动客户端静默发送:\r\n {appName} --ip <IP> [-app] --name <姓名> --addr <位置> [--desc <备注>]");
            sb.AppendLine();
            Console.WriteLine(sb.ToString());
            MessageBox.Show(sb.ToString(), "帮助");
        }

        private static void Send(bool app, string ip, string name, string addr, string desc)
        {
            //取数据
            var data = MyData.Get(app);
            data.Set(name, addr, desc);

            //发送
            var xml = data.ToXml();
            NetCli.Send(xml, ip, NetSvr.Port);
        }

    }
}
