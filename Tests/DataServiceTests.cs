using System;
using System.IO;
using System.Linq;
using Xunit;

namespace HW_info.Tests
{
    public class DataServiceTests : IDisposable
    {
        private readonly string _dbPath;

        public DataServiceTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"hw_info_test_{Guid.NewGuid()}.db");
            DataService.Initialize(_dbPath);
        }

        public void Dispose()
        {
            DataService.Shutdown();
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }

        [Fact]
        public void AddAndRetrieveRecord()
        {
            var data = new MyData
            {
                MAC地址 = "AA:BB:CC:DD:EE:FF",
                计算机名 = "PC-TEST",
                提交时间 = DateTime.Now
            };
            DataService.Add(data);
            var all = DataService.GetAll();
            Assert.Single(all);
        }

        [Fact]
        public void GetLatestByMac_ReturnsMostRecent()
        {
            var mac = "11:22:33:44:55:66";
            var earlier = new MyData { MAC地址 = mac, 计算机名 = "PC-OLD", 提交时间 = DateTime.Now.AddDays(-1) };
            var later = new MyData { MAC地址 = mac, 计算机名 = "PC-NEW", 提交时间 = DateTime.Now };
            DataService.Add(earlier);
            DataService.Add(later);
            var result = DataService.GetLatestByMac(mac);
            Assert.Equal("PC-NEW", result.计算机名);
        }

        [Fact]
        public void GetLatest_ReturnsOnePerMac()
        {
            DataService.Add(new MyData { MAC地址 = "MAC1", 计算机名 = "PC1-A", 提交时间 = DateTime.Now.AddDays(-1) });
            DataService.Add(new MyData { MAC地址 = "MAC1", 计算机名 = "PC1-B", 提交时间 = DateTime.Now });
            DataService.Add(new MyData { MAC地址 = "MAC2", 计算机名 = "PC2", 提交时间 = DateTime.Now });
            var result = DataService.GetLatest();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void ExportCsv_IncludesAllFields()
        {
            DataService.Add(new MyData
            {
                MAC地址 = "AA:BB:CC:DD:EE:FF",
                计算机名 = "PC-TEST",
                用户名 = "User1",
                提交时间 = new DateTime(2025, 1, 15, 10, 30, 0)
            });
            var csv = DataService.ExportCsv();
            Assert.Contains("计算机名", csv);
            Assert.Contains("PC-TEST", csv);
        }

        [Fact]
        public void GetByMac_ReturnsAllRecords()
        {
            var mac = "99:88:77:66:55:44";
            DataService.Add(new MyData { MAC地址 = mac, 计算机名 = "PC-A", 提交时间 = DateTime.Now.AddDays(-1) });
            DataService.Add(new MyData { MAC地址 = mac, 计算机名 = "PC-B", 提交时间 = DateTime.Now });
            var result = DataService.GetByMac(mac);
            Assert.Equal(2, result.Count);
        }
    }
}
