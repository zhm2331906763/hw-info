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
                    RunConsoleServer();
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
                Application.Run(new Form1Cli(ip, app));
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
                var cliApp = Regex.IsMatch(fileName, "app", RegexOptions.IgnoreCase);
                var cliIp = IPAddress.Broadcast.ToString();
                var match = Regex.Match(fileName, @"\d{8,}");
                if (match.Success && IPAddress.TryParse(match.Value, out var cliAddr))
                    cliIp = cliAddr.ToString();
                Application.Run(new Form1Cli(cliIp, cliApp));
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

        static void RunConsoleServerWithTray()
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
            sb.AppendLine("服务端参数:");
            sb.AppendLine($"  {appName} -svr                   启动服务端(控制台模式)");
            sb.AppendLine($"  {appName} -svr -service          启动服务端(Windows服务模式)");
            sb.AppendLine($"  {appName} -install               安装Windows服务");
            sb.AppendLine($"  {appName} -uninstall             卸载Windows服务");
            sb.AppendLine($"  {appName} -tray                  显示系统托盘");
            sb.AppendLine();
            sb.AppendLine("客户端参数:");
            sb.AppendLine($"  {appName} --ip <IP> [-app]       启动客户端");
            sb.AppendLine($"  {appName} --ip <IP> [-app] --name <姓名> --addr <位置> [--desc <备注>]");
            sb.AppendLine();
            sb.AppendLine("示例:");
            sb.AppendLine($"  {appName} -svr");
            sb.AppendLine($"  {appName} -install");
            Console.WriteLine(sb.ToString());
            MessageBox.Show(sb.ToString(), "帮助");
        }
    }
}
