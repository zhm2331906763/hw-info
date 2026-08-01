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
        public void GetChanges_Detects_DiskChangeViaPipe()
        {
            //模拟完整数据流：获取旧数据→写入新数据→对比变更
            var mac = "TEST-PIPE-001";
            var first = new MyData
            {
                MAC地址 = mac, 计算机名 = "PC1",
                CPU = "i5", 内存 = "8GB", 硬盘 = "256GB",
            };
            first.提交时间 = DateTime.Now;

            var oldData = ChangeTracker.GetChanges(null, first).ToList();
            Assert.Empty(oldData); //首次无变化

            var second = new MyData
            {
                MAC地址 = mac, 计算机名 = "PC1",
                CPU = "i5", 内存 = "8GB", 硬盘 = "256GB|1TB",
            };
            second.提交时间 = DateTime.Now;

            var changes = ChangeTracker.GetChanges(first, second).ToList();
            Assert.Single(changes);
            Assert.Equal("硬盘", changes[0].FieldName);
            Assert.Equal("256GB", changes[0].OldValue);
            Assert.Equal("256GB|1TB", changes[0].NewValue);
        }

        [Fact]
        public void GetChanges_Detects_MultipleChanges()
        {
            var oldData = new MyData
            {
                MAC地址 = "M1", CPU = "i5", 内存 = "8GB", 硬盘 = "256GB", 显卡 = "GTX 1060",
            };
            var newData = new MyData
            {
                MAC地址 = "M1", CPU = "i7", 内存 = "16GB", 硬盘 = "256GB|1TB", 显卡 = "GTX 1060",
            };

            var changes = ChangeTracker.GetChanges(oldData, newData).ToList();
            Assert.Equal(3, changes.Count);
            Assert.Contains(changes, c => c.FieldName == "CPU");
            Assert.Contains(changes, c => c.FieldName == "内存");
            Assert.Contains(changes, c => c.FieldName == "硬盘");
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
