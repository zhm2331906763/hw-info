using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;

namespace HW_info
{
    public class Sysinfo
    {

        /// <summary>
        /// WMI 
        /// </summary>
        /// <param name="queryString"></param>
        /// <returns></returns>
        private static string GetWmiInfo(string queryString)
        {

            List<string> list = new List<string>();
            try
            {
                ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(queryString);
                foreach (ManagementObject mo in managementObjectSearcher.Get())
                {
                    List<string> list2 = new List<string>();
                    foreach (var prop in mo.Properties)
                    {
                        if (prop.Value != null)
                        {
                            //ip 为 数组 
                            var v = prop.IsArray ? string.Join("|", prop.Value as string[]) : prop.Value;
                            //容量处理：硬盘用1000(厂商标称)，内存用1024(二进制)
                            if (prop.Name == "Size" || prop.Name == "Capacity")
                            {
                                long.TryParse(v.ToString(), out long size);
                                int divisor = prop.Name == "Size" ? 1000 : 1024;
                                string[] unit = { "B", "K", "M", "G", "T", "P" };
                                int i = 0;
                                while (size >= divisor && i < unit.Length)
                                {
                                    size /= divisor;
                                    i++;
                                }
                                v = $"[{size}{unit[i]}]";
                            }
                            //内存速率补单位
                            if (prop.Name == "Speed" || prop.Name == "ConfiguredClockSpeed")
                            {
                                var sv = Convert.ToString(v).Trim();
                                if (!string.IsNullOrEmpty(sv) && sv != "0" && !sv.Contains("MHz"))
                                    v = $"{sv}MHz";
                            }
                            var value = Convert.ToString(v).Trim();
                            list2.Add(value);
                        }
                        //硬盘大小放前面, 反序
                        if (prop.Name == "Size") list2.Reverse();
                    }
                    list.Add(string.Join(" ", list2));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return string.Join("|", list);
        }

        /// <summary>
        /// 计算机名
        /// </summary>
        /// <returns></returns>
        public static string GetMachineNames() => Environment.MachineName;

        /// <summary>
        /// 用户名
        /// </summary>
        /// <returns></returns>
        public static string GetUserName() => Environment.UserName;


        /// <summary>
        /// 产品
        /// </summary>
        /// <returns></returns>
        public static string GetProductInfo()
        {
            var sb = new StringBuilder();
            using (RegistryKey Key = Registry.LocalMachine)
            {
                var myReg = Key.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\BIOS");
                var value = myReg?.GetValue("SystemProductName");
                sb.Append(value);
            }
            return sb.ToString();
        }

        /// <summary>
        /// BIOS日期
        /// </summary>
        /// <returns></returns>
        public static string GetBiosDate()
        {
            var sb = new StringBuilder();
            using (RegistryKey Key = Registry.LocalMachine)
            {
                var myReg = Key.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\BIOS");
                var value = myReg?.GetValue("BIOSReleaseDate");
                sb.Append(value);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 操作系统
        /// </summary>
        /// <returns></returns>
        public static string GetOsinfo()
        {
            string queryString = "SELECT Caption,osarchitecture,version FROM Win32_OperatingSystem";
            return GetWmiInfo(queryString);
        }

        /// <summary>
        /// 系统安装日期
        /// </summary>
        /// <returns></returns>
        public static string GetOsInstallDate()
        {
            string queryString = "SELECT InstallDate FROM Win32_OperatingSystem";
            var value = GetWmiInfo(queryString);   //20151030112006.000000+480
            return value.Length >= 8
                ? $"{value.Substring(4, 2)}/{value.Substring(6, 2)}/{value.Substring(0, 4)}"
                : string.Empty;
        }

        /// <summary>
        /// CPU
        /// </summary>
        /// <returns></returns>
        public static string GetCpuInfo()
        {
            string queryString = "SELECT Name FROM Win32_Processor";
            return GetWmiInfo(queryString);
        }

        /// <summary>
        /// 主板
        /// </summary>
        /// <returns></returns>
        public static string GetBaseBoardInfo()
        {
            string queryString = "SELECT Manufacturer,Product FROM Win32_BaseBoard";
            return GetWmiInfo(queryString);
        }

        /// <summary>
        /// 主板产商
        /// </summary>
        /// <returns></returns>
        public static string GetBaseBoardInfo2()
        {
            var sb = new StringBuilder();
            using (RegistryKey Key = Registry.LocalMachine)
            {
                var myReg = Key.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\BIOS");
                var value = myReg?.GetValue("BaseBoardManufacturer");
                sb.Append(value).Append(" ");
                value = myReg?.GetValue("BaseBoardProduct");
                sb.Append(value);
            }
            return sb.ToString();
        }

        /// <summary>
        /// BIOS序列号
        /// </summary>
        /// <returns></returns>
        public static string GetBiosSerialNumber()
        {
            string queryString = "SELECT SerialNumber FROM Win32_Bios";
            return GetWmiInfo(queryString);
        }

        /// <summary>
        /// 显卡
        /// </summary>
        /// <returns></returns>
        public static string GetVideoInfo()
        {
            //只保留独显：排除虚拟设备、远程桌面、CPU集显、微软基础显示
            string queryString = @"
                SELECT Caption FROM Win32_VideoController 
                WHERE NOT PNPDeviceID LIKE 'ROOT%'
                AND NOT Caption LIKE '%Virtual%'
                AND NOT Caption LIKE '%Remote%'
                AND NOT Caption LIKE '%RayLink%'
                AND NOT Caption LIKE '%Basic Display%'
                AND NOT Caption LIKE '%Microsoft%'
                AND NOT Caption LIKE 'Intel(R)%HD%'
                AND NOT Caption LIKE 'Intel(R)%Iris%'
                AND NOT Caption LIKE 'Intel(R)%UHD%'
                AND NOT Caption LIKE '%RDP%'";
            return GetWmiInfo(queryString);
        }



        /// <summary>
        /// DisplayPNPDeviceID
        /// </summary>
        /// <returns></returns>
        private static string[] GetDisplayPNPDeviceID()
        {
            List<string> pnpDevID = new List<string>();
            try
            {
                var displayKey = "SYSTEM\\CurrentControlSet\\Enum\\DISPLAY";
                var regkey = Registry.LocalMachine.OpenSubKey(displayKey);
                var displayID = regkey.GetSubKeyNames();
                foreach (var display in displayID)
                {
                    var regkey2 = regkey.OpenSubKey(display);
                    var deviceID = regkey2.GetSubKeyNames();
                    foreach (var device in deviceID)
                    {
                        var regkey3 = regkey2.OpenSubKey(device);
                        var controlkey = regkey3.OpenSubKey("Control");
                        if (controlkey != null)
                        {
                            pnpDevID.Add("DISPLAY\\" + display + "\\" + device);
                        }
                        regkey3.Close();
                    }
                    regkey2.Close();
                }
                regkey.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return pnpDevID.ToArray();
        }

        /// <summary>
        /// 显示器
        /// </summary>
        /// <returns></returns>
        public static string GetMonitorInfo()
        {
            /* 
            var deviceIDs = GetDisplayPNPDeviceID();
            if (deviceIDs.Length == 0)
            {
                //效率太低   win10最新版， 注册表可能找不到
                string queryString = "SELECT DeviceID FROM Win32_PnPSignedDriver WHERE DeviceClass = 'MONITOR'";
                deviceIDs = GetWmiInfo(queryString).Split('|');
            }*/

            //查询显示器设备，使用Win32_PnPEntity并筛选监视器类别  支持Windows 7系统上支持
            string queryString = "SELECT PNPDeviceID FROM Win32_PnPEntity WHERE ClassGuid = '{4d36e96e-e325-11ce-bfc1-08002be10318}'";
            var deviceIDs = GetWmiInfo(queryString).Split('|');

            List<string> list = new List<string>();

            foreach (var deviceID in deviceIDs)
            {
                try
                {
                    //读EEID数据
                    RegistryKey Key = Registry.LocalMachine;
                    var myReg = Key.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum\\" + deviceID + "\\Device Parameters");
                    var bytes = myReg.GetValue("EDID") as byte[];
                    myReg.Close();

                    byte[] flagByte = { 0x4B, 0x5D, 0x6F };      //三个标志位 
                    foreach (var fb in flagByte)
                    {
                        if (bytes?.Length > 0 && bytes[fb] == 0xFC)         //标志位为FC 表示是 Monitor Name
                        {
                            var byte2 = bytes.Skip(fb + 2).Take(13).ToArray();    //标志位 + 2 开始取13位
                            string info = Encoding.UTF8.GetString(byte2).Trim();   //转字符串，去空
                            list.Add(info);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }

            //虚拟机没有edid ，取win默认显示器名称
            return list.Count > 0
                ? string.Join("|", list)
                : GetWmiInfo("SELECT Name FROM Win32_DesktopMonitor");
        }

        /// <summary>
        /// 硬盘
        /// </summary>
        /// <returns></returns>
        public static string GetHardDiskInfo()
        {
            string queryString = "SELECT Caption,Size FROM Win32_DiskDrive WHERE MediaType LIKE 'Fixed hard disk%'";
            return GetWmiInfo(queryString);
        }

        /// <summary>
        /// 硬盘序列号
        /// </summary>
        /// <returns></returns>
        public static string GetHardDiskSN()
        {
            string queryString = "SELECT SerialNumber FROM Win32_DiskDrive WHERE MediaType LIKE 'Fixed hard disk%'";
            return GetWmiInfo(queryString);
        }

        /// <summary>
        /// 内存
        /// </summary>
        /// <returns></returns>
        public static string GetMemoryInfo()
        {
            string queryString = "SELECT Manufacturer,PartNumber,Capacity,Speed,ConfiguredClockSpeed FROM Win32_PhysicalMemory";
            var raw = GetWmiInfo(queryString);
            return raw.Replace(" 0MHz", "").Replace(" 0", "").Trim();
        }


        /// <summary>
        /// 网卡
        /// </summary>
        /// <returns></returns>
        public static string GetNetworkAdapterInfo()
        {
            //排除虚拟网卡 Not Caption LIKE '%virtual%' 
            string queryString = "SELECT Name FROM Win32_NetworkAdapter WHERE NetEnabled = true and Not Caption LIKE '%virtual%'";
            return GetWmiInfo(queryString);

        }

        /// <summary>
        /// IP地址
        /// </summary>
        /// <returns></returns>
        public static string GetIPInfo()
        {
            string queryString = "SELECT IPAddress FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = true and Not Caption LIKE '%virtual%'";
            string ips = GetWmiInfo(queryString);

            //匹配IPv4地址
            return string.Join("|", ips.Split('|').Where(i => Regex.Match(i, @"^(?=\d+\.){3}\d+").Success));
        }

        /// <summary>
        /// MAC地址
        /// </summary>
        /// <returns></returns>
        public static string GetMacInfo()
        {
            string queryString = "SELECT MACAddress FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = true and Not Caption LIKE '%virtual%'";
            return GetWmiInfo(queryString);
        }

        /// <summary>
        /// BIOS
        /// </summary>
        /// <returns></returns>
        public static string GetBiosInfo()
        {
            string queryString = "SELECT Version,ReleaseDate FROM Win32_BIOS";
            return GetWmiInfo(queryString);
        }

        /// <summary>
        /// 打印机
        /// </summary>
        /// <returns></returns>
        public static string GetPrintInfo()
        {
            string queryString = "SELECT name FROM Win32_Printer WhERE PortName LIKE 'USB%'";
            return GetWmiInfo(queryString);
        }

        /// <summary>
        /// 应用程序
        /// </summary>
        /// <returns></returns>
        public static string GetRegApp()
        {
            var paths = new string[] {
                    @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

            var list = new List<string>();
            using (RegistryKey Key = Registry.LocalMachine)
            {
                foreach (var path in paths)
                {
                    try
                    {
                        var myReg = Key.OpenSubKey(path);
                        var keyNames = myReg.GetSubKeyNames();
                        foreach (var keyName in keyNames)
                        {
                            var subkey = myReg.OpenSubKey(keyName);
                            var displayName = subkey.GetValue("DisplayName");
                            //var installDate = subkey.GetValue("InstallDate");
                            if (displayName != null)
                            {
                                var name = displayName.ToString().Trim().Trim(new[] { '\0' });  //部分软件无耻，添加空字符
                                list.Add(name);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //throw;
                    }
                }
            }
            list.Sort();
            return string.Join("|", list.Distinct());
        }

    }
}
