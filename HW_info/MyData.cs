using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HW_info
{

    public class MyData : IEqualityComparer<MyData>
    {
        public string 计算机名 { get; set; }
        public string 用户名 { get; set; }
        public string 操作系统 { get; set; }
        public string 系统安装日期 { get; set; }
        public string 型号 { get; set; }
        public string BIOS日期 { get; set; }
        public string 序列号 { get; set; }
        public string CPU { get; set; }
        public string 主板 { get; set; }
        public string 内存 { get; set; }
        public string 硬盘 { get; set; }
        public string 显卡 { get; set; }
        public string 显示器 { get; set; }
        public string 打印机 { get; set; }
        public string 网卡 { get; set; }
        public string IP地址 { get; set; }
        public string MAC地址 { get; set; }
        public string 应用程序 { get; set; }
        public string 姓名 { get; set; }
        public string 位置 { get; set; }
        public string 备注 { get; set; }
        public DateTime 提交时间 { get; set; }


        public void Set(string 姓名, string 位置, string 备注 = null)
        {
            this.姓名 = 姓名;
            this.位置 = 位置;
            this.备注 = 备注;
        }

        public static MyData Get(bool app)
        {
            return new MyData
            {
                计算机名 = Sysinfo.GetMachineNames(),
                用户名 = Sysinfo.GetUserName(),
                操作系统 = Sysinfo.GetOsinfo(),
                系统安装日期 = Sysinfo.GetOsInstallDate(),
                型号 = Sysinfo.GetProductInfo(),
                BIOS日期 = Sysinfo.GetBiosDate(),
                序列号 = Sysinfo.GetBiosSerialNumber(),
                CPU = Sysinfo.GetCpuInfo(),
                主板 = Sysinfo.GetBaseBoardInfo(),
                内存 = Sysinfo.GetMemoryInfo(),
                硬盘 = Sysinfo.GetHardDiskInfo(),
                显卡 = Sysinfo.GetVideoInfo(),
                显示器 = Sysinfo.GetMonitorInfo(),
                打印机 = Sysinfo.GetPrintInfo(),
                网卡 = Sysinfo.GetNetworkAdapterInfo(),
                IP地址 = Sysinfo.GetIPInfo(),
                MAC地址 = Sysinfo.GetMacInfo(),
                应用程序 = app ? Sysinfo.GetRegApp() : null,
                提交时间 = DateTime.Now,
            };
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("计算机名：").AppendLine(计算机名);
            sb.Append("用户名：").AppendLine(用户名);
            sb.Append("操作系统：").AppendLine(操作系统);
            sb.Append("系统安装日期：").AppendLine(系统安装日期);
            sb.Append("型号：").AppendLine(型号);
            sb.Append("BIOS日期：").AppendLine(BIOS日期);
            sb.Append("序列号：").AppendLine(序列号);
            sb.Append("CPU：").AppendLine(CPU);
            sb.Append("主板：").AppendLine(主板);
            sb.Append("内存：").AppendLine(内存);
            sb.Append("硬盘：").AppendLine(硬盘);
            sb.Append("显卡：").AppendLine(显卡);
            sb.Append("显示器：").AppendLine(显示器);
            sb.Append("打印机：").AppendLine(打印机);
            sb.Append("网卡：").AppendLine(网卡);
            sb.Append("IP地址：").AppendLine(IP地址);
            sb.Append("MAC地址：").AppendLine(MAC地址);
            sb.Append("应用程序：").AppendLine(应用程序);
            return sb.ToString();
        }

        public string ToString2()
        {
            var sb = new StringBuilder();
            sb.Append(ToString());
            sb.Append("姓名：").AppendLine(姓名);
            sb.Append("位置：").AppendLine(位置);
            sb.Append("备注：").AppendLine(备注);
            sb.Append("提交时间：").AppendLine(提交时间.ToString("g"));
            return sb.ToString();
        }

        public string ToXml()
        {
            return XmlConvert.Serializer(this);
        }

        public string ToJson()
        {
            return JsonConvert.Serialize(this);
        }

        public bool Equals(MyData x, MyData y)
        {
            return x.ToString() == y.ToString()
                && x.姓名 == y.姓名
                && x.位置 == y.位置
                && x.备注 == y.备注;
        }

        public int GetHashCode(MyData obj)
        {
            return (IP地址 + MAC地址).GetHashCode();
        }

    }
}
