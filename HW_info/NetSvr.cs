using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace HW_info
{
    public class NetSvr
    {
        volatile bool runing = true;

        public class NetEventArgs : EventArgs
        {
            public Byte[] Buffer { get; set; }
            public IPEndPoint IPEnd { get; set; }
            public override string ToString() => IPEnd.ToString();
        }

        /// <summary>
        /// 数据接收事件
        /// </summary>
        public event EventHandler<NetEventArgs> NetEvent;

        /// <summary>
        /// 回复数据
        /// </summary>
        public static byte[] ReplyOk => Encoding.UTF8.GetBytes("Ok");


        /// <summary>
        /// 服务端口
        /// </summary>
        public static int Port { get; set; } = 51528;

        /// <summary>
        /// 启动服务
        /// </summary>
        public void Start()
        {
            //启动UDP服务
            Task.Run(() => UdpSvr(Port)).ConfigureAwait(false);
            //启动Web服务
            Task.Run(() => WebSvr(Port)).ConfigureAwait(false);
        }

        /// <summary>
        /// 关闭服务
        /// </summary>
        public void Close() => runing = false;


        /// <summary>
        /// UDP服务
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task UdpSvr(int port)
        {
            var udpServer = new UdpClient(port);
            Console.WriteLine("UdpServer Start...");

            while (runing)
            {
                var result = await udpServer.ReceiveAsync();
                _ = Task.Run(() =>
                  {
                      try
                      {
                          var bytes = result.Buffer;
                          var ipEnd = result.RemoteEndPoint;
                          var args = new NetEventArgs { Buffer = bytes, IPEnd = ipEnd };
                          Console.WriteLine($"UdpReceive：{args}");
                          udpServer.Send(ReplyOk, ReplyOk.Length, ipEnd);
                          NetEvent?.BeginInvoke(this, args, null, null);
                      }
                      catch (Exception)
                      {
                          //throw;
                      }
                  });
            }

            udpServer.Close();
        }

        /// <summary>
        /// TCP服务
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task TcpSvr(int port)
        {
            var tcpServer = new TcpListener(IPAddress.Any, port);
            tcpServer.Start();
            Console.WriteLine("TcpServer Start...");

            while (runing)
            {
                var tcpClient = await tcpServer.AcceptTcpClientAsync();
                _ = Task.Run(() =>
                {
                    try
                    {
                        using (var ns = tcpClient.GetStream())
                        {
                            var bytes = ns.ReadBytes();
                            var ipEnd = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
                            var args = new NetEventArgs { Buffer = bytes, IPEnd = ipEnd };
                            Console.WriteLine($"TcpReceive：{args}");
                            ns.WriteBytes(ReplyOk);
                            NetEvent?.BeginInvoke(this, args, null, null);
                        }
                    }
                    finally
                    {
                        tcpClient.Close();
                    }
                });
            }

            tcpServer.Stop();
        }

        public WebApi WebApi { get; set; }

        public async Task WebSvr(int port)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{port}/");
            listener.Start();
            Console.WriteLine("WebServer Start...");
            while (runing)
            {
                var context = await listener.GetContextAsync();
                _ = Task.Run(() =>
                {
                    try
                    {
                        if (WebApi != null)
                        {
                            var path = context.Request.Url.AbsolutePath;

                            //二进制导出（XLSX）
                            if (WebApi.TryGetBinaryExport(path, out var binData, out var binContentType))
                            {
                                var dateStr = DateTime.Now.ToString("yyyyMMdd");
                                context.Response.ContentType = binContentType;
                                var encodedName = Uri.EscapeDataString($"硬件资产信息表-{dateStr}.xlsx");
                                context.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{dateStr}.xlsx\"; filename*=UTF-8''{encodedName}";
                                context.Response.ContentLength64 = binData.Length;
                                using (var stream = context.Response.OutputStream)
                                    stream.Write(binData, 0, binData.Length);
                            }
                            else
                            {
                                var result = WebApi.HandleRequest(context.Request);

                                if (path == "/api/export/csv")
                                    context.Response.ContentType = "text/csv; charset=utf-8";
                                else if (path.EndsWith(".css"))
                                    context.Response.ContentType = "text/css; charset=utf-8";
                                else if (path.EndsWith(".js"))
                                    context.Response.ContentType = "application/javascript; charset=utf-8";
                                else if (path.EndsWith(".html") || path == "/")
                                    context.Response.ContentType = "text/html; charset=utf-8";
                                else
                                    context.Response.ContentType = "application/json; charset=utf-8";

                                var bytes = Encoding.UTF8.GetBytes(result);
                                context.Response.ContentLength64 = bytes.Length;
                                using (var stream = context.Response.OutputStream)
                                    stream.Write(bytes, 0, bytes.Length);
                            }
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                        }
                    }
                    finally
                    {
                        context.Response.Close();
                    }
                });
            }
            listener.Close();
        }

    }



    public static class MyExtensions
    {
        /// <summary>
        /// 写数据，一个int32的后续数据大小
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bytes"></param>
        public static void WriteBytes(this NetworkStream stream, byte[] bytes)
        {
            var bw = new BinaryWriter(stream);
            bw.Write(bytes.Length);
            bw.Write(bytes);
            bw.Flush();
        }

        /// <summary>
        /// 读数据，一个int32的后续数据大小
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static byte[] ReadBytes(this NetworkStream stream)
        {
            var br = new BinaryReader(stream);
            var count = br.ReadInt32();
            return br.ReadBytes(count);
        }

    }



}

