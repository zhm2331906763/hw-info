using System;
using System.Linq;
using Xunit;

namespace HW_info.Tests
{
    public class ChangeTrackerTests
    {
        [Fact]
        public void GetChanges_DetectsChangedFields()
        {
            var oldData = new MyData
            {
                MAC地址 = "AA:BB:CC:DD:EE:FF",
                计算机名 = "PC-OLD",
                CPU = "Intel i5",
                内存 = "8GB",
                硬盘 = "256GB SSD"
            };
            var newData = new MyData
            {
                MAC地址 = "AA:BB:CC:DD:EE:FF",
                计算机名 = "PC-OLD",
                CPU = "Intel i7",
                内存 = "16GB",
                硬盘 = "256GB SSD"
            };

            var changes = ChangeTracker.GetChanges(oldData, newData).ToList();

            Assert.Contains(changes, c => c.FieldName == "CPU" && c.OldValue == "Intel i5" && c.NewValue == "Intel i7");
            Assert.Contains(changes, c => c.FieldName == "内存" && c.OldValue == "8GB" && c.NewValue == "16GB");
            Assert.DoesNotContain(changes, c => c.FieldName == "硬盘");
            Assert.Equal(2, changes.Count);
        }

        [Fact]
        public void GetChanges_NoChanges_ReturnsEmpty()
        {
            var data = new MyData
            {
                MAC地址 = "AA:BB:CC:DD:EE:FF",
                计算机名 = "PC",
                CPU = "Intel i5",
                内存 = "8GB"
            };

            var changes = ChangeTracker.GetChanges(data, data).ToList();

            Assert.Empty(changes);
        }

        [Fact]
        public void GetChanges_IgnoresSpecifiedFields()
        {
            var oldData = new MyData
            {
                MAC地址 = "AA:BB:CC:DD:EE:FF",
                计算机名 = "PC",
                姓名 = "OldName",
                位置 = "OldLocation",
                备注 = "OldRemark",
                提交时间 = new DateTime(2024, 1, 1)
            };
            var newData = new MyData
            {
                MAC地址 = "AA:BB:CC:DD:EE:FF",
                计算机名 = "PC",
                姓名 = "NewName",
                位置 = "NewLocation",
                备注 = "NewRemark",
                提交时间 = new DateTime(2025, 1, 1)
            };

            var changes = ChangeTracker.GetChanges(oldData, newData).ToList();

            Assert.Empty(changes);
        }

        [Fact]
        public void GetChanges_NullOldData_ReturnsEmpty()
        {
            var newData = new MyData
            {
                MAC地址 = "AA:BB:CC:DD:EE:FF",
                计算机名 = "PC"
            };

            var changes = ChangeTracker.GetChanges(null, newData).ToList();

            Assert.Empty(changes);
        }
    }
}
