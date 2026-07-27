using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace HW_info
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                MainInner(args);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败：{ex.Message}\n\n{ex.GetType().Name}\n{ex.StackTrace}", "HW-info 错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void MainInner(string[] args)
        {
            if (args.Length > 0)
            {
                var argSet = new HashSet<string>(args, StringComparer.OrdinalIgnoreCase);
                var argDict = args
                    .Select((a, i) => new { a, i })
                    .Where(x => x.a.StartsWith("--") && x.i + 1 < args.Length)
                    .ToDictionary(x => x.a.Substring(2), x => args[x.i + 1]);

                if (argSet.Contains("-svr") && argSet.Contains("-service"))
                {
                    ServiceBase.Run(new HwService());
                    return;
                }

                if (argSet.Contains("-install"))
                {
                    ServiceManager.Install();
                    Console.WriteLine("Service installed.");
                    return;
                }

                if (argSet.Contains("-uninstall"))
                {
                    ServiceManager.Uninstall();
                    Console.WriteLine("Service uninstalled.");
                    return;
                }

                if (argSet.Contains("-tray"))
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new TrayForm());
                    return;
                }

                if (argSet.Contains("-svr"))
                {
                    RunConsoleServerWithTray();
                    return;
                }

                if (argSet.Contains("-?") || argSet.Contains("-h"))
                {
                    ShowHelp();
                    return;
                }

                var app = argSet.Contains("-app") || argSet.Contains("--app");
                var ip = IPAddress.Broadcast.ToString();
                if (argDict.TryGetValue("ip", out var ipStr) && IPAddress.TryParse(ipStr, out var addr))
                    ip = addr.ToString();
                if (argDict.TryGetValue("name", out var name) && argDict.TryGetValue("addr", out var addr2))
                {
                    argDict.TryGetValue("desc", out var desc);
                    SendData(app, ip, name, addr2, desc);
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                //命令行指定了IP但没有姓名/位置，视为配置错误，打开服务器配置
                Application.Run(new ServerConfigForm());
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var fileName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            if (Regex.IsMatch(fileName, "-svr", RegexOptions.IgnoreCase))
            {
                RunConsoleServerWithTray();
            }
            else
            {
                //默认模式：打开服务器配置界面
                Application.Run(new ServerConfigForm());
            }
        }

        static void RunConsoleServer()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HW_info.db");
            DataService.Initialize(dbPath);

            var api = new WebApi();
            var svr = new NetSvr { WebApi = api };
            svr.NetEvent += (s, e) =>
            {
                var text = Encoding.UTF8.GetString(e.Buffer);
                var myData = XmlConvert.Deserialize<MyData>(text) ?? JsonConvert.Deserialize<MyData>(text);
                if (myData != null)
                {
                    myData.提交时间 = DateTime.Now;
                    DataService.Add(myData);
                    ChangeTracker.ProcessNewData(myData);
                    Console.WriteLine($"Received: {myData.计算机名} ({myData.MAC地址})");
                }
            };
            svr.Start();
            Console.WriteLine($"HW-info server running on port {NetSvr.Port}...");
            Console.WriteLine("Press Ctrl+C to stop.");
            var evt = new ManualResetEvent(false);
            Console.CancelKeyPress += (s, e) => { e.Cancel = true; evt.Set(); };
            evt.WaitOne();
            svr.Close();
            DataService.Shutdown();
        }

        public static void RunConsoleServerWithTray()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HW_info.db");
            DataService.Initialize(dbPath);

            var api = new WebApi();
            var svr = new NetSvr { WebApi = api };
            svr.NetEvent += (s, e) =>
            {
                var text = Encoding.UTF8.GetString(e.Buffer);
                var myData = XmlConvert.Deserialize<MyData>(text) ?? JsonConvert.Deserialize<MyData>(text);
                if (myData != null)
                {
                    myData.提交时间 = DateTime.Now;
                    DataService.Add(myData);
                    ChangeTracker.ProcessNewData(myData);
                }
            };
            svr.Start();
            Application.Run(new TrayForm());
        }

        static void SendData(bool app, string ip, string name, string addr, string desc)
        {
            var data = MyData.Get(app);
            data.Set(name, addr, desc);
            var xml = data.ToXml();
            NetCli.Send(xml, ip, NetSvr.Port);
        }

        static void ShowHelp()
        {
            var appName = AppDomain.CurrentDomain.FriendlyName;
            var sb = new StringBuilder();
            sb.AppendLine("HW-info 资产管理工具 v2.0");
            sb.AppendLine();
            sb.AppendLine("服务端:");
            sb.AppendLine($"  {appName} -svr              启动服务端(带托盘图标)");
            sb.AppendLine($"  {appName} -install          安装Windows服务(开机自启)");
            sb.AppendLine($"  {appName} -uninstall        卸载Windows服务");
            sb.AppendLine();
            sb.AppendLine("客户端:");
            sb.AppendLine($"  {appName} --ip <IP>         启动客户端界面");
            sb.AppendLine($"  {appName} --ip <IP> -app    启动客户端(收集软件列表)");
            sb.AppendLine($"  {appName} --ip <IP> --name <姓名> --addr <位置>  静默提交");
            sb.AppendLine();
            sb.AppendLine("文件名配置(客户端):");
            sb.AppendLine($"  HW-info-<十进制IP>[-app].exe");
            sb.AppendLine($"  例: HW-info-3232235876.exe  → 提交到 192.168.1.100");
            sb.AppendLine();
            sb.AppendLine("管理后台: http://本机IP:51528");
            Console.WriteLine(sb.ToString());
            MessageBox.Show(sb.ToString(), "帮助");
        }
    }
}
