using System;

namespace HW_info
{
    public static class ServiceManager
    {
        public static void Install() => Console.WriteLine("Service installed.");
        public static void Uninstall() => Console.WriteLine("Service uninstalled.");
    }
}
