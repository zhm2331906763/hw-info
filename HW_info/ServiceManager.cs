using System;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;

namespace HW_info
{
    public static class ServiceManager
    {
        private static string ServiceName => "HWInfoSvr";
        private static string DisplayName => "HW-info 资产管理服务";

        public static bool IsInstalled()
        {
            try
            {
                foreach (var s in ServiceController.GetServices())
                    if (s.ServiceName == ServiceName) return true;
            }
            catch { }
            return false;
        }

        public static ServiceControllerStatus? GetStatus()
        {
            try
            {
                using (var sc = new ServiceController(ServiceName))
                    return sc.Status;
            }
            catch { return null; }
        }

        public static void Install()
        {
            var exePath = Assembly.GetExecutingAssembly().Location;
            var psi = new ProcessStartInfo
            {
                FileName = "sc",
                Arguments = $"create \"{ServiceName}\" binPath=\"{exePath} -svr -service\" start=auto displayName=\"{DisplayName}\"",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            Process.Start(psi)?.WaitForExit();

            psi = new ProcessStartInfo
            {
                FileName = "sc",
                Arguments = $"description \"{ServiceName}\" \"收集电脑硬件信息的资产管理系统服务端\"",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            Process.Start(psi)?.WaitForExit();
        }

        public static void Uninstall()
        {
            try
            {
                using (var sc = new ServiceController(ServiceName))
                {
                    if (sc.Status != ServiceControllerStatus.Stopped)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                }
            }
            catch { }

            var psi = new ProcessStartInfo
            {
                FileName = "sc",
                Arguments = $"delete \"{ServiceName}\"",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            Process.Start(psi)?.WaitForExit();
        }

        public static void StartService()
        {
            try
            {
                using (var sc = new ServiceController(ServiceName))
                    if (sc.Status == ServiceControllerStatus.Stopped)
                        sc.Start();
            }
            catch { }
        }

        public static void StopService()
        {
            try
            {
                using (var sc = new ServiceController(ServiceName))
                    if (sc.Status == ServiceControllerStatus.Running)
                        sc.Stop();
            }
            catch { }
        }
    }
}
