using System;
using System.Collections.Generic;
using System.Linq;

namespace HW_info
{
    public static class Data1
    {
        public static IEnumerable<MyData> Datas => DataService.GetAll();
        public static int Count => DataService.GetTotalCount();
        public static IEnumerable<MyData> Distinct => DataService.GetLatest();
        public static IEnumerable<MyData> Latest => DataService.GetLatest();

        public static void Add(MyData data)
        {
            if (data != null)
            {
                data.提交时间 = DateTime.Now;
                DataService.Add(data);
                ChangeTracker.ProcessNewData(data);
            }
        }

        public static string ToCsv(IEnumerable<MyData> datas)
        {
            return DataService.ExportCsv();
        }

        public static string ToHtml(IEnumerable<MyData> datas) => null;
    }
}
