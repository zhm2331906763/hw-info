using System;
using System.IO;
using System.IO.Pipes;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace HW_info
{
    public class HwService : ServiceBase
    {
        private NetSvr _svr;
        private CancellationTokenSource _cts;

        public HwService()
        {
            ServiceName = "HWInfoSvr";
            CanStop = true;
            CanShutdown = true;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            _cts = new CancellationTokenSource();
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HW_info.db");
            DataService.Initialize(dbPath);

            var api = new WebApi();
            _svr = new NetSvr { WebApi = api };
            _svr.NetEvent += OnNetEvent;
            _svr.Start();

            _ = StartPipeServer(_cts.Token);
        }

        protected override void OnStop()
        {
            _cts?.Cancel();
            _svr?.Close();
            DataService.Shutdown();
        }

        protected override void OnShutdown() => OnStop();

        private void OnNetEvent(object sender, NetSvr.NetEventArgs e)
        {
            try
            {
                var text = System.Text.Encoding.UTF8.GetString(e.Buffer);
                var myData = XmlConvert.Deserialize<MyData>(text) ?? JsonConvert.Deserialize<MyData>(text);
                if (myData != null)
                {
                    myData.提交时间 = DateTime.Now;
                    DataService.Add(myData);
                    ChangeTracker.ProcessNewData(myData);
                }
            }
            catch { }
        }

        private async Task StartPipeServer(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using (var pipe = new NamedPipeServerStream("HWInfoSvc", PipeDirection.InOut, 1))
                    {
                        await pipe.WaitForConnectionAsync(ct).ConfigureAwait(false);
                        var reader = new StreamReader(pipe);
                        var writer = new StreamWriter(pipe) { AutoFlush = true };
                        var cmd = await reader.ReadLineAsync().ConfigureAwait(false);

                        switch (cmd)
                        {
                            case "status":
                                await writer.WriteLineAsync($"{{\"running\":true,\"count\":{DataService.GetTotalCount()}}}").ConfigureAwait(false);
                                break;
                            case "stop":
                                OnStop();
                                await writer.WriteLineAsync("{\"stopped\":true}").ConfigureAwait(false);
                                break;
                            case "restart":
                                OnStop();
                                OnStart(new string[0]);
                                await writer.WriteLineAsync("{\"restarted\":true}").ConfigureAwait(false);
                                break;
                            default:
                                await writer.WriteLineAsync("{\"error\":\"unknown command\"}").ConfigureAwait(false);
                                break;
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { }
            }
        }
    }
}
