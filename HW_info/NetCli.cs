using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HW_info
{
    public class NetCli
    {
        /// <summary>
        /// 超时
        /// </summary>
        public const int timeout = 2000;

        /// <summary>
        /// UDP发送
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string UdpSend(string text, string host, int port)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                var result = UdpSend(bytes, host, port);
                return Encoding.UTF8.GetString(result);
            }
            catch (Exception)
            {
                return null;
            }
        }

        static byte[] UdpSend(byte[] bytes, string host, int port)
        {
            using (var udp = new UdpClient())
            {
                udp.Client.SendTimeout = timeout;
                udp.Client.ReceiveTimeout = timeout;
                udp.Send(bytes, bytes.Length, host, port);
                IPEndPoint ipEnd = null;
                return udp.Receive(ref ipEnd);
            }
        }

        /// <summary>
        /// TCP发送
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string TcpSend(string text, string host, int port)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                var result = TcpSend(bytes, host, port);
                return Encoding.UTF8.GetString(result);
            }
            catch (Exception)
            {
                return null;
            }
        }

        static byte[] TcpSend(byte[] bytes, string host, int port)
        {
            using (var tcp = new TcpClient())
            {
                tcp.Client.SendTimeout = timeout;
                tcp.Client.ReceiveTimeout = timeout;
                Task.WaitAny(Task.Run(() => tcp.Connect(host, port)), Task.Delay(timeout));
                using (var ns = tcp.GetStream())
                {
                    ns.WriteBytes(bytes);
                    return ns.ReadBytes();
                }
            }
        }

        /// <summary>
        /// WEB发送
        /// </summary>
        /// <param name="text"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string WebSend(string text, string url)
        {
            string result = null;
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    client.Headers[HttpRequestHeader.ContentType] = "text/plain; charset=utf-8";
                    var task = Task.Run(() => result = client.UploadString(url, text));
                    Task.WaitAny(task, Task.Delay(timeout));
                }
            }
            catch (Exception)
            {
                //
            }
            return result;
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="text"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool Send(string text, string ip, int port)
        {
            string result = UdpSend(text, ip, port);
            string url = $"http://{ip}:{NetSvr.Port}/api/datas/add";
            //text = JsonConvert.Serialize(XmlConvert.Deserialize<MyData>(text));
            if (result == null) result = WebSend(text, url);
            return result != null;
        }

    }

}

