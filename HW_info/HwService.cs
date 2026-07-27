using System.ServiceProcess;

namespace HW_info
{
    public class HwService : ServiceBase
    {
        public HwService()
        {
            ServiceName = "HWInfoSvr";
        }
    }
}
