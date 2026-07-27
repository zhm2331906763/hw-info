using System;
using System.Collections.Generic;
using System.Reflection;

namespace HW_info
{
    public static class ChangeTracker
    {
        private static readonly HashSet<string> IgnoredFields = new HashSet<string>
        {
            "姓名", "位置", "备注", "提交时间"
        };

        public static IEnumerable<ChangeLog> GetChanges(MyData oldData, MyData newData)
        {
            if (oldData == null || newData == null)
                yield break;

            var props = typeof(MyData).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                if (IgnoredFields.Contains(prop.Name))
                    continue;

                var oldVal = prop.GetValue(oldData) as string ?? "";
                var newVal = prop.GetValue(newData) as string ?? "";

                if (oldVal != newVal)
                {
                    yield return new ChangeLog
                    {
                        MacAddress = newData.MAC地址 ?? "",
                        ComputerName = newData.计算机名 ?? "",
                        FieldName = prop.Name,
                        OldValue = string.IsNullOrEmpty(oldVal) ? null : oldVal,
                        NewValue = string.IsNullOrEmpty(newVal) ? null : newVal,
                        ChangedAt = DateTime.Now,
                    };
                }
            }
        }

        public static List<ChangeLog> ProcessNewData(MyData newData)
        {
            if (string.IsNullOrEmpty(newData?.MAC地址))
                return new List<ChangeLog>();

            var oldData = DataService.GetLatestByMac(newData.MAC地址);
            var changes = new List<ChangeLog>(GetChanges(oldData, newData));

            if (changes.Count > 0)
                DataService.AddChangeLogs(changes);

            return changes;
        }
    }
}
