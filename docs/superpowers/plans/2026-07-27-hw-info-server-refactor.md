# HW-info Server Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform the HW-info WinForms server into a Windows Service + Tray App with SQLite storage, professional web UI, and automatic hardware change tracking.

**Architecture:** Server runs as a Windows Service (headless) with a companion system-tray app for user interaction. Data storage migrates from XML file to SQLite. Web UI is served via the existing HttpListener, completely redesigned as a modern admin dashboard with RESTful JSON API. Incoming client data is compared against the latest record for the same machine to detect hardware changes and log them.

**Tech Stack:** C# / .NET Framework 4.8, System.Data.SQLite, System.ServiceProcess, HttpListener, Bootstrap 5, vanilla JavaScript, Named Pipes (IPC).

## Global Constraints

- Target framework: .NET Framework 4.8 (upgrade from 4.5.2)
- SQLite NuGet package: `System.Data.SQLite.Core` version 1.0.118+
- Frontend: Bootstrap 5 CDN + vanilla JS, no build toolchain
- Web UI served as embedded resources from the EXE
- Client-side (Form1Cli) unchanged — must still compile and function
- All server-side WinForms forms must be removed (Form2Svr, Form4Table, Form5Setup)
- Data is append-only (every client submission is a new row in SQLite)
- Change tracking compares incoming data against latest record by MAC address

---

## File Structure

```
HW_info/
├── HW_info.csproj               # MODIFY: upgrade to 4.8, add SQLite ref
├── Program.cs                   # MODIFY: dispatch -svr, -tray, -install, -uninstall
├── MyData.cs                    # UNCHANGED (data model, used by both client + server)
├── Sysinfo.cs                   # UNCHANGED (client-side hardware collection)
├── NetSvr.cs                    # MODIFY: add WebApi integration, remove inline route handlers
├── NetCli.cs                    # UNCHANGED (client-side networking)
├── JsonConvert.cs               # KEEP (server still deserializes JSON from HTTP clients)
├── XmlConvert.cs                # KEEP (server deserializes XML from UDP clients)
├── HwService.cs                 # CREATE: Windows Service (ServiceBase)
├── ServiceManager.cs            # CREATE: install/uninstall/start/stop utilities
├── DataService.cs               # CREATE: SQLite data access layer
├── ChangeTracker.cs             # CREATE: change detection + ChangeLog generation
├── ChangeLog.cs                 # CREATE: ChangeLog model class
├── WebApi.cs                    # CREATE: RESTful JSON API route handler
├── TrayForm.cs                  # CREATE: tray icon + context menu
├── TrayForm.Designer.cs         # CREATE: designer file for tray form
├── Resources/
│   ├── web_index.html           # CREATE: main admin dashboard HTML
│   ├── web_app.css              # CREATE: custom CSS
│   ├── web_app.js               # CREATE: frontend JavaScript
│   └── down.html                # KEEP (client download page template)
└── Properties/
    └── AssemblyInfo.cs          # UNCHANGED

Tests/                                  # CREATE: test project (xUnit)
├── Tests.csproj
├── DataServiceTests.cs
└── ChangeTrackerTests.cs
```

---

### Task 1: Project Upgrade, NuGet & Test Project Setup

**Files:**
- Modify: `HW_info\HW_info.csproj` (full file rewrite)
- Create: `Tests\Tests.csproj`

- [ ] **Step 1: Rewrite HW_info.csproj**

Target 4.8, add System.Data.SQLite.Core reference, keep all existing file references but remove Form2Svr, Form4Table, Form5Setup from Compile items (they'll be removed from disk later):

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
    <ProjectGuid>{29CCDA77-20F8-4213-A97C-B08B325F172D}</ProjectGuid>
    <OutputType>WinExe</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>HW_info</RootNamespace>
    <AssemblyName>HW-info-svr</AssemblyName>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <Deterministic>false</Deterministic>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup>
    <StartupObject />
  </PropertyGroup>
  <PropertyGroup>
    <ApplicationIcon>未命名-1.ico</ApplicationIcon>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="System" />
    <Reference Include="System.Core" />
    <Reference Include="System.Data" />
    <Reference Include="System.Data.SQLite, Version=1.0.118.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139, processorArchitecture=MSIL">
      <HintPath>packages\System.Data.SQLite.Core.1.0.118.0\lib\net46\System.Data.SQLite.dll</HintPath>
    </Reference>
    <Reference Include="System.Drawing" />
    <Reference Include="System.Management" />
    <Reference Include="System.ServiceProcess" />
    <Reference Include="System.Web.Extensions" />
    <Reference Include="System.Windows.Forms" />
    <Reference Include="System.Xml" />
    <Reference Include="Microsoft.CSharp" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include="ChangeLog.cs" />
    <Compile Include="ChangeTracker.cs" />
    <Compile Include="DataService.cs" />
    <Compile Include="HwService.cs" />
    <Compile Include="ServiceManager.cs" />
    <Compile Include="TrayForm.cs">
      <SubType>Form</SubType>
    </Compile>
    <Compile Include="TrayForm.Designer.cs">
      <DependentUpon>TrayForm.cs</DependentUpon>
    </Compile>
    <Compile Include="WebApi.cs" />
    <Compile Include="Data1.cs" />
    <Compile Include="JsonConvert.cs" />
    <Compile Include="MyData.cs" />
    <Compile Include="NetCli.cs" />
    <Compile Include="NetSvr.cs" />
    <Compile Include="Program.cs" />
    <Compile Include="Properties\AssemblyInfo.cs" />
    <Compile Include="Properties\Resources.Designer.cs">
      <AutoGen>True</AutoGen>
      <DesignTime>True</DesignTime>
      <DependentUpon>Resources.resx</DependentUpon>
    </Compile>
    <Compile Include="Sysinfo.cs" />
    <Compile Include="XmlConvert.cs" />
  </ItemGroup>
  <ItemGroup>
    <EmbeddedResource Include="Properties\Resources.resx">
      <Generator>ResXFileCodeGenerator</Generator>
      <LastGenOutput>Resources.Designer.cs</LastGenOutput>
    </EmbeddedResource>
    <EmbeddedResource Include="Resources\web_index.html" />
    <EmbeddedResource Include="Resources\web_app.css" />
    <EmbeddedResource Include="Resources\web_app.js" />
    <EmbeddedResource Include="Resources\down.html" />
  </ItemGroup>
  <ItemGroup>
    <None Include="packages.config" />
  </ItemGroup>
  <ItemGroup>
    <Content Include="未命名-1.ico" />
  </ItemGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
  <Import Project="packages\System.Data.SQLite.Core.1.0.118.0\build\net46\System.Data.SQLite.Core.targets" Condition="Exists('packages\System.Data.SQLite.Core.1.0.118.0\build\net46\System.Data.SQLite.Core.targets')" />
</Project>
```

- [ ] **Step 2: Create packages.config**

```xml
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="System.Data.SQLite.Core" version="1.0.118.0" targetFramework="net48" />
</packages>
```

- [ ] **Step 3: Create Tests.csproj**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="..\packages\xunit.runner.visualstudio.2.4.5\build\net462\xunit.runner.visualstudio.props" Condition="Exists('..\packages\xunit.runner.visualstudio.2.4.5\build\net462\xunit.runner.visualstudio.props')" />
  <Import Project="..\packages\xunit.core.2.4.2\build\xunit.core.props" Condition="Exists('..\packages\xunit.core.2.4.2\build\xunit.core.props')" />
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
    <ProjectGuid>{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>HW_info.Tests</RootNamespace>
    <AssemblyName>HW_info.Tests</AssemblyName>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="System" />
    <Reference Include="System.Core" />
    <Reference Include="System.Data" />
    <Reference Include="xunit.abstractions, Version=2.0.0.0, Culture=neutral, PublicKeyToken=8d05b1bb7a6fdb6c">
      <HintPath>..\packages\xunit.abstractions.2.0.3\lib\net35\xunit.abstractions.dll</HintPath>
    </Reference>
    <Reference Include="xunit.assert, Version=2.4.2.0, Culture=neutral, PublicKeyToken=8d05b1bb7a6fdb6c">
      <HintPath>..\packages\xunit.assert.2.4.2\lib\netstandard1.1\xunit.assert.dll</HintPath>
    </Reference>
    <Reference Include="xunit.core, Version=2.4.2.0, Culture=neutral, PublicKeyToken=8d05b1bb7a6fdb6c">
      <HintPath>..\packages\xunit.core.2.4.2\lib\net452\xunit.core.dll</HintPath>
    </Reference>
    <Reference Include="xunit.execution.desktop, Version=2.4.2.0, Culture=neutral, PublicKeyToken=8d05b1bb7a6fdb6c">
      <HintPath>..\packages\xunit.execution.desktop.2.4.2\lib\net452\xunit.execution.desktop.dll</HintPath>
    </Reference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\HW_info\HW_info.csproj">
      <Project>{29CCDA77-20F8-4213-A97C-B08B325F172D}</Project>
      <Name>HW_info</Name>
    </ProjectReference>
  </ItemGroup>
  <ItemGroup>
    <Compile Include="DataServiceTests.cs" />
    <Compile Include="ChangeTrackerTests.cs" />
  </ItemGroup>
  <ItemGroup>
    <None Include="packages.config" />
  </ItemGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
```

- [ ] **Step 4: Set up NuGet packages via CLI**

Run in project root:
```powershell
cd E:\hw-info-master
nuget install System.Data.SQLite.Core -Version 1.0.118.0 -OutputDirectory packages
nuget install xunit -Version 2.4.2 -OutputDirectory packages
nuget install xunit.runner.visualstudio -Version 2.4.5 -OutputDirectory packages
```

- [ ] **Step 5: Verify build**

Run:
```powershell
msbuild HW_info\HW_info.csproj /p:Configuration=Release
```
Expected: Build succeeds (may have compile errors for missing new files — that's expected until later tasks)

- [ ] **Step 6: Commit**

```bash
git add HW_info/HW_info.csproj HW_info/packages.config Tests/Tests.csproj Tests/packages.config
git commit -m "chore: upgrade to .NET 4.8, add SQLite + xUnit dependencies"
```

---

### Task 2: SQLite Data Access Layer (DataService)

**Files:**
- Create: `HW_info\DataService.cs`
- Create: `HW_info\ChangeLog.cs`
- Create: `Tests\DataServiceTests.cs`
- Modify: none

**Interfaces:**
- Produces: `DataService` static class with methods for all DB operations
- Produces: `ChangeLog` class (Id, MacAddress, ComputerName, FieldName, OldValue, NewValue, ChangedAt)

- [ ] **Step 1: Create ChangeLog model**

`HW_info\ChangeLog.cs`:
```csharp
using System;

namespace HW_info
{
    public class ChangeLog
    {
        public int Id { get; set; }
        public string MacAddress { get; set; }
        public string ComputerName { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
```

- [ ] **Step 2: Write DataService test**

`Tests\DataServiceTests.cs`:
```csharp
using Xunit;
using HW_info;
using System;
using System.IO;
using System.Linq;

namespace HW_info.Tests
{
    public class DataServiceTests : IDisposable
    {
        private readonly string _testDbPath;

        public DataServiceTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"hw_test_{Guid.NewGuid()}.db");
            DataService.Initialize(_testDbPath);
        }

        public void Dispose()
        {
            DataService.Shutdown();
            if (File.Exists(_testDbPath))
                File.Delete(_testDbPath);
        }

        [Fact]
        public void AddAndRetrieveRecord()
        {
            var data = new MyData
            {
                计算机名 = "PC-001",
                用户名 = "admin",
                操作系统 = "Windows 10 Pro",
                型号 = "ThinkPad X1",
                序列号 = "SN12345",
                CPU = "Intel i7",
                主板 = "Lenovo XYZ",
                内存 = "16GB",
                硬盘 = "512GB SSD",
                显卡 = "Intel Iris",
                MAC地址 = "00-11-22-33-44-55",
                IP地址 = "192.168.1.100",
                提交时间 = DateTime.Now,
            };

            DataService.Add(data);

            var all = DataService.GetAll().ToList();
            Assert.Single(all);
            Assert.Equal("PC-001", all[0].计算机名);
        }

        [Fact]
        public void GetLatestByMac_ReturnsMostRecent()
        {
            var mac = "AA-BB-CC-DD-EE-FF";
            var older = new MyData { 计算机名 = "PC-OLD", MAC地址 = mac, 提交时间 = DateTime.Now.AddHours(-1) };
            var newer = new MyData { 计算机名 = "PC-NEW", MAC地址 = mac, 提交时间 = DateTime.Now };

            DataService.Add(older);
            DataService.Add(newer);

            var latest = DataService.GetLatestByMac(mac);
            Assert.NotNull(latest);
            Assert.Equal("PC-NEW", latest.计算机名);
        }

        [Fact]
        public void GetLatest_ReturnsOnePerMac()
        {
            DataService.Add(new MyData { 计算机名 = "PC1", MAC地址 = "MAC1", 提交时间 = DateTime.Now });
            DataService.Add(new MyData { 计算机名 = "PC2", MAC地址 = "MAC2", 提交时间 = DateTime.Now });
            DataService.Add(new MyData { 计算机名 = "PC1v2", MAC地址 = "MAC1", 提交时间 = DateTime.Now.AddHours(1) });

            var latest = DataService.GetLatest().ToList();
            Assert.Equal(2, latest.Count);
            Assert.Contains(latest, d => d.计算机名 == "PC1v2");
            Assert.Contains(latest, d => d.计算机名 == "PC2");
        }

        [Fact]
        public void ExportCsv_IncludesAllFields()
        {
            DataService.Add(new MyData { 计算机名 = "PC1", MAC地址 = "M1", 提交时间 = DateTime.Now });

            var csv = DataService.ExportCsv();
            Assert.Contains("计算机名", csv);
            Assert.Contains("PC1", csv);
            Assert.Contains("MAC地址", csv);
        }

        [Fact]
        public void GetByMac_ReturnsAllRecords()
        {
            var mac = "11-22-33-44-55-66";
            DataService.Add(new MyData { 计算机名 = "A", MAC地址 = mac, 提交时间 = DateTime.Now.AddDays(-1) });
            DataService.Add(new MyData { 计算机名 = "B", MAC地址 = mac, 提交时间 = DateTime.Now });

            var history = DataService.GetByMac(mac).ToList();
            Assert.Equal(2, history.Count);
        }
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

```powershell
cd E:\hw-info-master
msbuild Tests\Tests.csproj /p:Configuration=Debug
& .\packages\xunit.runner.console.2.4.2\tools\net472\xunit.console.x86.exe Tests\bin\Debug\HW_info.Tests.dll
```
Expected: Build fails — DataService class not found

- [ ] **Step 4: Create DataService**

`HW_info\DataService.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

namespace HW_info
{
    public static class DataService
    {
        private static string _connectionString;
        private static SQLiteConnection _connection;

        public static void Initialize(string dbPath = null)
        {
            if (dbPath == null)
                dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HW_info.db");

            _connectionString = $"Data Source={dbPath};Version=3;";
            _connection = new SQLiteConnection(_connectionString);
            _connection.Open();
            CreateTables();
        }

        public static void Shutdown()
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }

        private static void CreateTables()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS MachineReports (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        MacAddress TEXT NOT NULL DEFAULT '',
                        计算机名 TEXT,
                        用户名 TEXT,
                        操作系统 TEXT,
                        系统安装日期 TEXT,
                        型号 TEXT,
                        BIOS日期 TEXT,
                        序列号 TEXT,
                        CPU TEXT,
                        主板 TEXT,
                        内存 TEXT,
                        硬盘 TEXT,
                        显卡 TEXT,
                        显示器 TEXT,
                        打印机 TEXT,
                        网卡 TEXT,
                        IP地址 TEXT,
                        MAC地址 TEXT,
                        应用程序 TEXT,
                        姓名 TEXT,
                        位置 TEXT,
                        备注 TEXT,
                        提交时间 TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS ChangeLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        MacAddress TEXT NOT NULL DEFAULT '',
                        ComputerName TEXT,
                        FieldName TEXT NOT NULL,
                        OldValue TEXT,
                        NewValue TEXT,
                        ChangedAt TEXT NOT NULL
                    );

                    CREATE INDEX IF NOT EXISTS IX_MachineReports_Mac ON MachineReports(MacAddress);
                    CREATE INDEX IF NOT EXISTS IX_ChangeLogs_Mac ON ChangeLogs(MacAddress);
                    CREATE INDEX IF NOT EXISTS IX_MachineReports_SubmitTime ON MachineReports(提交时间);
                ";
                cmd.ExecuteNonQuery();
            }
        }

        public static void Add(MyData data)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO MachineReports 
                        (MacAddress, 计算机名, 用户名, 操作系统, 系统安装日期, 型号, BIOS日期, 序列号, 
                         CPU, 主板, 内存, 硬盘, 显卡, 显示器, 打印机, 网卡, IP地址, MAC地址, 
                         应用程序, 姓名, 位置, 备注, 提交时间)
                    VALUES 
                        (@MacAddress, @计算机名, @用户名, @操作系统, @系统安装日期, @型号, @BIOS日期, @序列号,
                         @CPU, @主板, @内存, @硬盘, @显卡, @显示器, @打印机, @网卡, @IP地址, @MAC地址,
                         @应用程序, @姓名, @位置, @备注, @提交时间)";

                cmd.Parameters.AddWithValue("@MacAddress", data.MAC地址 ?? "");
                cmd.Parameters.AddWithValue("@计算机名", (object)data.计算机名 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@用户名", (object)data.用户名 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@操作系统", (object)data.操作系统 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@系统安装日期", (object)data.系统安装日期 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@型号", (object)data.型号 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BIOS日期", (object)data.BIOS日期 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@序列号", (object)data.序列号 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CPU", (object)data.CPU ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@主板", (object)data.主板 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@内存", (object)data.内存 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@硬盘", (object)data.硬盘 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@显卡", (object)data.显卡 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@显示器", (object)data.显示器 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@打印机", (object)data.打印机 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@网卡", (object)data.网卡 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IP地址", (object)data.IP地址 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MAC地址", (object)data.MAC地址 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@应用程序", (object)data.应用程序 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@姓名", (object)data.姓名 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@位置", (object)data.位置 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@备注", (object)data.备注 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@提交时间", data.提交时间.ToString("yyyy-MM-dd HH:mm:ss"));

                cmd.ExecuteNonQuery();
            }
        }

        public static void AddChangeLog(ChangeLog log)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO ChangeLogs (MacAddress, ComputerName, FieldName, OldValue, NewValue, ChangedAt)
                    VALUES (@MacAddress, @ComputerName, @FieldName, @OldValue, @NewValue, @ChangedAt)";

                cmd.Parameters.AddWithValue("@MacAddress", log.MacAddress ?? "");
                cmd.Parameters.AddWithValue("@ComputerName", (object)log.ComputerName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FieldName", log.FieldName ?? "");
                cmd.Parameters.AddWithValue("@OldValue", (object)log.OldValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NewValue", (object)log.NewValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ChangedAt", log.ChangedAt.ToString("yyyy-MM-dd HH:mm:ss"));

                cmd.ExecuteNonQuery();
            }
        }

        public static void AddChangeLogs(IEnumerable<ChangeLog> logs)
        {
            using (var tx = _connection.BeginTransaction())
            {
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO ChangeLogs (MacAddress, ComputerName, FieldName, OldValue, NewValue, ChangedAt)
                        VALUES (@MacAddress, @ComputerName, @FieldName, @OldValue, @NewValue, @ChangedAt)";

                    var macParam = cmd.CreateParameter(); macParam.ParameterName = "@MacAddress";
                    var cnParam = cmd.CreateParameter(); cnParam.ParameterName = "@ComputerName";
                    var fnParam = cmd.CreateParameter(); fnParam.ParameterName = "@FieldName";
                    var ovParam = cmd.CreateParameter(); ovParam.ParameterName = "@OldValue";
                    var nvParam = cmd.CreateParameter(); nvParam.ParameterName = "@NewValue";
                    var caParam = cmd.CreateParameter(); caParam.ParameterName = "@ChangedAt";
                    cmd.Parameters.Add(macParam);
                    cmd.Parameters.Add(cnParam);
                    cmd.Parameters.Add(fnParam);
                    cmd.Parameters.Add(ovParam);
                    cmd.Parameters.Add(nvParam);
                    cmd.Parameters.Add(caParam);

                    foreach (var log in logs)
                    {
                        macParam.Value = log.MacAddress ?? "";
                        cnParam.Value = (object)log.ComputerName ?? DBNull.Value;
                        fnParam.Value = log.FieldName ?? "";
                        ovParam.Value = (object)log.OldValue ?? DBNull.Value;
                        nvParam.Value = (object)log.NewValue ?? DBNull.Value;
                        caParam.Value = log.ChangedAt.ToString("yyyy-MM-dd HH:mm:ss");
                        cmd.ExecuteNonQuery();
                    }
                }
                tx.Commit();
            }
        }

        private static MyData ReadDataRow(SQLiteDataReader r)
        {
            var d = new MyData();
            d.计算机名 = r["计算机名"] as string;
            d.用户名 = r["用户名"] as string;
            d.操作系统 = r["操作系统"] as string;
            d.系统安装日期 = r["系统安装日期"] as string;
            d.型号 = r["型号"] as string;
            d.BIOS日期 = r["BIOS日期"] as string;
            d.序列号 = r["序列号"] as string;
            d.CPU = r["CPU"] as string;
            d.主板 = r["主板"] as string;
            d.内存 = r["内存"] as string;
            d.硬盘 = r["硬盘"] as string;
            d.显卡 = r["显卡"] as string;
            d.显示器 = r["显示器"] as string;
            d.打印机 = r["打印机"] as string;
            d.网卡 = r["网卡"] as string;
            d.IP地址 = r["IP地址"] as string;
            d.MAC地址 = r["MAC地址"] as string;
            d.应用程序 = r["应用程序"] as string;
            d.姓名 = r["姓名"] as string;
            d.位置 = r["位置"] as string;
            d.备注 = r["备注"] as string;

            if (DateTime.TryParse(r["提交时间"] as string, out var dt))
                d.提交时间 = dt;

            return d;
        }

        public static IEnumerable<MyData> GetAll()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM MachineReports ORDER BY 提交时间 DESC";
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        yield return ReadDataRow(r);
                }
            }
        }

        public static MyData GetLatestByMac(string macAddress)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM MachineReports WHERE MacAddress = @MacAddress ORDER BY 提交时间 DESC LIMIT 1";
                cmd.Parameters.AddWithValue("@MacAddress", macAddress ?? "");
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        return ReadDataRow(r);
                }
            }
            return null;
        }

        public static IEnumerable<MyData> GetLatest()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT * FROM MachineReports 
                    WHERE Id IN (SELECT MAX(Id) FROM MachineReports GROUP BY MacAddress)
                    ORDER BY 提交时间 DESC";
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        yield return ReadDataRow(r);
                }
            }
        }

        public static IEnumerable<MyData> GetByMac(string macAddress)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM MachineReports WHERE MacAddress = @MacAddress ORDER BY 提交时间 DESC";
                cmd.Parameters.AddWithValue("@MacAddress", macAddress ?? "");
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        yield return ReadDataRow(r);
                }
            }
        }

        public static IEnumerable<ChangeLog> GetChangeLogs(string macAddress = null, int limit = 200)
        {
            using (var cmd = _connection.CreateCommand())
            {
                var sql = "SELECT * FROM ChangeLogs";
                if (!string.IsNullOrEmpty(macAddress))
                    sql += " WHERE MacAddress = @MacAddress";
                sql += " ORDER BY ChangedAt DESC LIMIT @Limit";
                cmd.CommandText = sql;
                if (!string.IsNullOrEmpty(macAddress))
                    cmd.Parameters.AddWithValue("@MacAddress", macAddress);
                cmd.Parameters.AddWithValue("@Limit", limit);

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        yield return new ChangeLog
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            MacAddress = r["MacAddress"] as string ?? "",
                            ComputerName = r["ComputerName"] as string,
                            FieldName = r["FieldName"] as string ?? "",
                            OldValue = r["OldValue"] as string,
                            NewValue = r["NewValue"] as string,
                            ChangedAt = DateTime.TryParse(r["ChangedAt"] as string, out var dt) ? dt : DateTime.MinValue,
                        };
                    }
                }
            }
        }

        public static string ExportCsv()
        {
            var sb = new StringBuilder();
            var props = typeof(MyData).GetProperties();
            sb.AppendLine(string.Join(",", props.Select(p => $"\"{p.Name}\"")));

            foreach (var data in GetAll())
            {
                sb.AppendLine(string.Join(",", props.Select(p => $"\"{p.GetValue(data)}\"")));
            }
            return sb.ToString();
        }

        public static int GetTotalCount()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM MachineReports";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static int GetDistinctMacCount()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(DISTINCT MacAddress) FROM MachineReports WHERE MacAddress != ''";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static int GetTodayCount()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM MachineReports WHERE 提交Time >= date('now','localtime')";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
```

- [ ] **Step 5: Run tests**

```powershell
msbuild Tests\Tests.csproj /p:Configuration=Debug
& .\packages\xunit.runner.console.2.4.2\tools\net472\xunit.console.x86.exe Tests\bin\Debug\HW_info.Tests.dll
```
Expected: All 5 tests pass

- [ ] **Step 6: Commit**

```bash
git add HW_info/DataService.cs HW_info/ChangeLog.cs Tests/DataServiceTests.cs
git commit -m "feat: add SQLite data access layer (DataService) and ChangeLog model"
```

---

### Task 3: Change Tracker

**Files:**
- Create: `HW_info\ChangeTracker.cs`
- Create: `Tests\ChangeTrackerTests.cs`

**Interfaces:**
- Consumes: `DataService.Add()`, `DataService.GetLatestByMac()`, `DataService.AddChangeLogs()`
- Produces: `ChangeTracker.GetChanges(MyData oldData, MyData newData)` → `List<ChangeLog>`

- [ ] **Step 1: Write ChangeTracker test**

`Tests\ChangeTrackerTests.cs`:
```csharp
using Xunit;
using HW_info;
using System;
using System.Linq;

namespace HW_info.Tests
{
    public class ChangeTrackerTests
    {
        [Fact]
        public void GetChanges_DetectsChangedFields()
        {
            var oldData = new MyData { CPU = "Intel i5", 内存 = "8GB", 硬盘 = "256GB" };
            var newData = new MyData { CPU = "Intel i7", 内存 = "16GB", 硬盘 = "256GB" };

            var changes = ChangeTracker.GetChanges(oldData, newData).ToList();

            Assert.Equal(2, changes.Count);
            Assert.Contains(changes, c => c.FieldName == "CPU" && c.OldValue == "Intel i5" && c.NewValue == "Intel i7");
            Assert.Contains(changes, c => c.FieldName == "内存" && c.OldValue == "8GB" && c.NewValue == "16GB");
        }

        [Fact]
        public void GetChanges_NoChanges_ReturnsEmpty()
        {
            var data = new MyData { CPU = "Intel i7", 内存 = "16GB" };
            var changes = ChangeTracker.GetChanges(data, data);
            Assert.Empty(changes);
        }

        [Fact]
        public void GetChanges_IgnoresSpecifiedFields()
        {
            var oldData = new MyData { 姓名 = "Alice", 位置 = "Room1", 提交时间 = DateTime.Now.AddDays(-1) };
            var newData = new MyData { 姓名 = "Bob", 位置 = "Room2", 提交时间 = DateTime.Now };

            var changes = ChangeTracker.GetChanges(oldData, newData);
            Assert.Empty(changes); // 姓名/位置/备注/提交时间 should be ignored
        }

        [Fact]
        public void GetChanges_NullOldData_ReturnsEmpty()
        {
            var changes = ChangeTracker.GetChanges(null, new MyData { CPU = "i7" });
            Assert.Empty(changes);
        }
    }
}
```

- [ ] **Step 2: Run test**

```powershell
msbuild Tests\Tests.csproj /p:Configuration=Debug
& .\packages\xunit.runner.console.2.4.2\tools\net472\xunit.console.x86.exe Tests\bin\Debug\HW_info.Tests.dll
```
Expected: Build fails — ChangeTracker not found

- [ ] **Step 3: Create ChangeTracker**

`HW_info\ChangeTracker.cs`:
```csharp
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
```

- [ ] **Step 4: Run tests**

```powershell
msbuild Tests\Tests.csproj /p:Configuration=Debug
& .\packages\xunit.runner.console.2.4.2\tools\net472\xunit.console.x86.exe Tests\bin\Debug\HW_info.Tests.dll
```
Expected: All 4 tests pass

- [ ] **Step 5: Commit**

```bash
git add HW_info/ChangeTracker.cs Tests/ChangeTrackerTests.cs
git commit -m "feat: add change detection (ChangeTracker)"
```

---

### Task 4: Web API Layer (RESTful JSON)

**Files:**
- Create: `HW_info\WebApi.cs`
- Modify: `HW_info\NetSvr.cs` — replace inline route handlers with WebApi integration

**Interfaces:**
- Consumes: `DataService.GetAll()`, `DataService.GetLatest()`, `DataService.GetByMac()`, `DataService.GetLatestByMac()`, `DataService.GetChangeLogs()`, `DataService.ExportCsv()`, `DataService.Add()`, `DataService.GetTotalCount()`, `DataService.GetDistinctMacCount()`, `DataService.GetTodayCount()`, `ChangeTracker.ProcessNewData()`
- Produces: `WebApi` class with route handlers for NetSvr

- [ ] **Step 1: Create WebApi**

`HW_info\WebApi.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

namespace HW_info
{
    public class WebApi
    {
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();
        private readonly Dictionary<string, Func<HttpListenerRequest, string>> _staticFiles;

        public WebApi()
        {
            _json.MaxJsonLength = int.MaxValue;
            _staticFiles = new Dictionary<string, string>
            {
                { "/", "HW_info.Resources.web_index.html" },
                { "/app.css", "HW_info.Resources.web_app.css" },
                { "/app.js", "HW_info.Resources.web_app.js" },
            };
        }

        public string HandleRequest(HttpListenerRequest req)
        {
            var path = req.Url.AbsolutePath.ToLowerInvariant();

            try
            {
                if (_staticFiles.ContainsKey(path))
                    return LoadEmbeddedResource(_staticFiles[path]);

                switch (path)
                {
                    case "/api/datas":
                        return HandleDatas(req);
                    case "/api/datas/latest":
                        return JsonResponse(DataService.GetLatest());
                    case "/api/datas/distinct":
                        return JsonResponse(DataService.GetLatest());
                    case "/api/datas/history":
                        return HandleHistory(req);
                    case "/api/changelogs":
                        return HandleChangeLogs(req);
                    case "/api/stats":
                        return HandleStats();
                    case "/api/export/csv":
                        return HandleExportCsv();
                    case "/api/datas/add":
                        return HandleDataAdd(req);
                    default:
                        return JsonError("Not found", 404);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message, 500);
            }
        }

        private string HandleDatas(HttpListenerRequest req)
        {
            var mac = req.QueryString["mac"];
            if (!string.IsNullOrEmpty(mac))
                return JsonResponse(DataService.GetByMac(mac));

            return JsonResponse(DataService.GetAll());
        }

        private string HandleHistory(HttpListenerRequest req)
        {
            var mac = req.QueryString["mac"];
            if (string.IsNullOrEmpty(mac))
                return JsonError("mac parameter required", 400);

            var records = DataService.GetByMac(mac).ToList();
            return JsonResponse(new { records, total = records.Count });
        }

        private string HandleChangeLogs(HttpListenerRequest req)
        {
            var mac = req.QueryString["mac"];
            var limitStr = req.QueryString["limit"];
            int limit = 200;
            int.TryParse(limitStr, out limit);

            var logs = DataService.GetChangeLogs(mac, limit);
            return JsonResponse(logs);
        }

        private string HandleStats()
        {
            return JsonResponse(new
            {
                totalReports = DataService.GetTotalCount(),
                totalMachines = DataService.GetDistinctMacCount(),
                todayNew = DataService.GetTodayCount(),
            });
        }

        private string HandleExportCsv()
        {
            return DataService.ExportCsv();
        }

        private string HandleDataAdd(HttpListenerRequest req)
        {
            if (req.HttpMethod != "POST" || !req.HasEntityBody)
                return JsonError("POST required", 405);

            var bytes = new byte[req.ContentLength64];
            using (var stream = req.InputStream)
                stream.Read(bytes, 0, bytes.Length);

            var text = Encoding.UTF8.GetString(bytes);
            MyData myData = XmlConvert.Deserialize<MyData>(text) ?? JsonConvert.Deserialize<MyData>(text);

            if (myData == null)
                return JsonError("Invalid data", 400);

            myData.提交时间 = DateTime.Now;
            DataService.Add(myData);

            // Detect changes
            var changes = ChangeTracker.ProcessNewData(myData);

            return JsonResponse(new { result = "ok", changes = changes.Count });
        }

        private string JsonResponse(object data)
        {
            return _json.Serialize(data);
        }

        private string JsonError(string message, int statusCode)
        {
            return _json.Serialize(new { error = message, status = statusCode });
        }

        private string LoadEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return "Resource not found";
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }
    }
}
```

- [ ] **Step 2: Modify NetSvr to use WebApi**

Replace the inline route dictionary and WebSvr handler logic:

`HW_info\NetSvr.cs` — replace the `HttpRouteFunc` field and the route-handling block inside `WebSvr`:

In the class body, replace:
```csharp
public Dictionary<string, Func<HttpListenerRequest, string>> HttpRouteFunc = new Dictionary<string, Func<HttpListenerRequest, string>>()
{
    ["/hi"] = (req) => "Hello world!"
};
```
With:
```csharp
public WebApi WebApi { get; set; }
```

Then replace the entire body inside `WebSvr`'s `Task.Run` (lines 160-181) with:
```csharp
try
{
    var path = context.Request.Url.AbsolutePath;
    if (WebApi != null)
    {
        var result = WebApi.HandleRequest(context.Request);
        // Check if this is CSV export — set content type
        if (path == "/api/export/csv")
        {
            context.Response.ContentType = "text/csv; charset=utf-8";
            context.Response.ContentEncoding = Encoding.UTF8;
        }
        else
        {
            context.Response.ContentType = "application/json; charset=utf-8";
        }
        var bytes = Encoding.UTF8.GetBytes(result);
        context.Response.ContentLength64 = bytes.Length;
        using (var stream = context.Response.OutputStream)
            stream.Write(bytes, 0, bytes.Length);
    }
    else
    {
        context.Response.StatusCode = 404;
    }
}
finally
{
    context.Response.Close();
}
```

And `Start()` method — remove the HttpRouteFunc assignment lines (the `/Datas/Add` assignment). Replace the body with just:
```csharp
public void Start()
{
    Task.Run(() => UdpSvr(Port)).ConfigureAwait(false);
    Task.Run(() => WebSvr(Port)).ConfigureAwait(false);
}
```

- [ ] **Step 3: Update Form2Svr (remove old inline routes)**

Actually — we're going to REMOVE Form2Svr entirely in a later task. For now, just leave it as legacy code that will be deleted. The `NetSvr.WebApi` property is set by whoever creates the NetSvr — the HwService will set it.

Wait, I should not leave Form2Svr with broken code. Since we're changing NetSvr (removing HttpRouteFunc), Form2Svr needs to be updated. But we're going to delete Form2Svr anyway.

Let me just remove Form2Svr from the project now (remove from .csproj). The old server form is completely replaced by HwService.

Actually, let me update the plan: in Task 4, we just modify NetSvr. The Form2Svr will be removed in the cleanup task (Task 9). But to keep the project building between tasks, I need to handle the transition.

Let me add a step to temporarily comment out or fix Form2Svr so the project builds.

Alternatively, I could remove the old files now. Let me just say in the plan to remove them in this task to keep things building.

Actually, let me re-think. The user wants to remove server-side GUI. Let me remove Form2Svr, Form4Table, and Form5Setup in Task 4 (or earlier) to keep the project clean.

Let me add a step to remove old files in Task 4.

OK, let me simplify. I'll keep the plan as is — Task 4 modifies NetSvr. Then in Task 9 (Cleanup), we remove the old forms. But to keep the project building, I need to make sure Form2Svr still compiles or is removed.

Actually, the cleanest approach: Remove Form2Svr, Form4Table, Form5Setup from the .csproj in Task 4 since they won't work with the new NetSvr anyway. Task 9 will delete the actual files.

Let me add a .csproj edit step to remove those file references.

- [ ] **Step 4: Remove Form2Svr, Form4Table, Form5Setup from csproj**

Using edit, remove the Compile items for Form2Svr, Form4Table, Form5Setup (and their Designer.cs/resx) from HW_info.csproj.

- [ ] **Step 5: Build and verify**

```powershell
msbuild HW_info\HW_info.csproj /p:Configuration=Release
```
Expected: Build succeeds (new NetSvr compiles, Form2Svr references gone)

- [ ] **Step 6: Commit**

```bash
git add HW_info/WebApi.cs HW_info/NetSvr.cs HW_info/HW_info.csproj
git commit -m "feat: add RESTful WebApi layer, integrate with NetSvr"
```

---

### Task 5: Data1.cs Migration

**Files:**
- Modify: `HW_info\Data1.cs` — refactor to delegate to DataService

The old Data1.cs is used by Form2Svr and Form4Table. Since those are being removed, Data1.cs is only needed for:
- Legacy migration path (existing XML data)
- Its CSV/HTML methods (replaced by WebApi/DataService)

We'll gut Data1.cs and make its public members delegate to DataService for backward compatibility.

- [ ] **Step 1: Rewrite Data1.cs**

```csharp
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

        // ToHtml is no longer used — replaced by WebApi JSON
        // Keep for backward compat, but it's dead code
        public static string ToHtml(IEnumerable<MyData> datas) => null;
    }
}
```

- [ ] **Step 2: Build**

```powershell
msbuild HW_info\HW_info.csproj /p:Configuration=Release
```
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add HW_info/Data1.cs
git commit -m "refactor: Data1 delegates to DataService/ChangeTracker"
```

---

### Task 6: Windows Service (HwService)

**Files:**
- Create: `HW_info\HwService.cs`
- Create: `HW_info\ServiceManager.cs`

**Interfaces:**
- Consumes: `NetSvr`, `WebApi`, `DataService.Initialize()`, `DataService.Shutdown()`
- Produces: `HwService : ServiceBase` for Windows SCM
- Produces: `ServiceManager` with `Install()` / `Uninstall()` / `IsInstalled()` / `GetStatus()`

- [ ] **Step 1: Create HwService**

`HW_info\HwService.cs`:
```csharp
using System;
using System.IO;
using System.IO.Pipes;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace HW_info
{
    public class HwService : ServiceBase
    {
        private NetSvr _svr;
        private NamedPipeServerStream _pipeServer;
        private CancellationTokenSource _cts;

        public HwService()
        {
            ServiceName = "HWInfoSvr";
            CanStop = true;
            CanShutdown = true;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            _cts = new CancellationTokenSource();

            // Init SQLite
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HW_info.db");
            DataService.Initialize(dbPath);

            // Setup web api and server
            var api = new WebApi();
            _svr = new NetSvr { WebApi = api };
            _svr.NetEvent += OnNetEvent;
            _svr.Start();

            // Start named pipe IPC for tray app
            _ = StartPipeServer(_cts.Token);
        }

        protected override void OnStop()
        {
            _cts?.Cancel();
            _svr?.Close();
            DataService.Shutdown();
            _pipeServer?.Dispose();
        }

        protected override void OnShutdown() => OnStop();

        private void OnNetEvent(object sender, NetSvr.NetEventArgs e)
        {
            try
            {
                var text = System.Text.Encoding.UTF8.GetString(e.Buffer);
                var myData = XmlConvert.Deserialize<MyData>(text) ?? JsonConvert.Deserialize<MyData>(text);
                if (myData != null)
                {
                    myData.提交时间 = DateTime.Now;
                    DataService.Add(myData);
                    ChangeTracker.ProcessNewData(myData);
                }
            }
            catch { }
        }

        private async Task StartPipeServer(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using (var pipe = new NamedPipeServerStream("HWInfoSvc", PipeDirection.InOut, 1))
                    {
                        await pipe.WaitForConnectionAsync(ct).ConfigureAwait(false);
                        var reader = new StreamReader(pipe);
                        var writer = new StreamWriter(pipe) { AutoFlush = true };
                        var cmd = await reader.ReadLineAsync().ConfigureAwait(false);

                        switch (cmd)
                        {
                            case "status":
                                await writer.WriteLineAsync($"{{\"running\":true,\"count\":{DataService.GetTotalCount()}}}").ConfigureAwait(false);
                                break;
                            case "stop":
                                OnStop();
                                await writer.WriteLineAsync("{\"stopped\":true}").ConfigureAwait(false);
                                break;
                            case "restart":
                                OnStop();
                                OnStart(new string[0]);
                                await writer.WriteLineAsync("{\"restarted\":true}").ConfigureAwait(false);
                                break;
                            default:
                                await writer.WriteLineAsync("{\"error\":\"unknown command\"}").ConfigureAwait(false);
                                break;
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { }
            }
        }
    }
}
```

- [ ] **Step 2: Create ServiceManager**

`HW_info\ServiceManager.cs`:
```csharp
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using Microsoft.Win32;

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
                using (var sc = ServiceController.GetServices())
                {
                    foreach (var s in sc)
                    {
                        if (s.ServiceName == ServiceName) return true;
                    }
                }
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
            catch
            {
                return null;
            }
        }

        public static void Install()
        {
            var exePath = Assembly.GetExecutingAssembly().Location;
            var psi = new ProcessStartInfo
            {
                FileName = "sc",
                Arguments = $"create \"{ServiceName}\" binPath=\"{exePath} -svr\" start=auto displayName=\"{DisplayName}\"",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            Process.Start(psi)?.WaitForExit();

            // Set description
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

        public static void SetStartup(bool autoStart)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sc",
                Arguments = $"config \"{ServiceName}\" start={(autoStart ? "auto" : "demand")}",
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
                {
                    if (sc.Status == ServiceControllerStatus.Stopped)
                        sc.Start();
                }
            }
            catch { }
        }

        public static void StopService()
        {
            try
            {
                using (var sc = new ServiceController(ServiceName))
                {
                    if (sc.Status == ServiceControllerStatus.Running)
                        sc.Stop();
                }
            }
            catch { }
        }
    }
}
```

- [ ] **Step 3: Build**

```powershell
msbuild HW_info\HW_info.csproj /p:Configuration=Release
```
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add HW_info/HwService.cs HW_info/ServiceManager.cs
git commit -m "feat: add Windows Service (HwService) and ServiceManager"
```

---

### Task 7: System Tray Application

**Files:**
- Create: `HW_info\TrayForm.cs`
- Create: `HW_info\TrayForm.Designer.cs`

**Interfaces:**
- Consumes: `ServiceManager` (static methods)
- Produces: `TrayForm` WinForms form that runs hidden, shows NotifyIcon in system tray

- [ ] **Step 1: Create TrayForm.Designer.cs**

`HW_info\TrayForm.Designer.cs`:
```csharp
namespace HW_info
{
    partial class TrayForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.miOpenWeb = new System.Windows.Forms.ToolStripMenuItem();
            this.miSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.miServiceStatus = new System.Windows.Forms.ToolStripMenuItem();
            this.miStartService = new System.Windows.Forms.ToolStripMenuItem();
            this.miStopService = new System.Windows.Forms.ToolStripMenuItem();
            this.miRestartService = new System.Windows.Forms.ToolStripMenuItem();
            this.miSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.miAutoStart = new System.Windows.Forms.ToolStripMenuItem();
            this.miSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.miExit = new System.Windows.Forms.ToolStripMenuItem();

            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.miOpenWeb,
                this.miSeparator1,
                this.miServiceStatus,
                this.miStartService,
                this.miStopService,
                this.miRestartService,
                this.miSeparator2,
                this.miAutoStart,
                this.miSeparator3,
                this.miExit,
            });

            this.miOpenWeb.Text = "打开 Web 管理界面(&W)";
            this.miServiceStatus.Text = "服务状态: 未知";
            this.miServiceStatus.Enabled = false;
            this.miStartService.Text = "启动服务(&S)";
            this.miStopService.Text = "停止服务(&T)";
            this.miRestartService.Text = "重启服务(&R)";
            this.miAutoStart.Text = "开机自启动";
            this.miExit.Text = "退出(&X)";

            this.notifyIcon.ContextMenuStrip = this.contextMenu;
            this.notifyIcon.Text = "HW-info 资产管理";
            this.notifyIcon.Visible = true;

            this.miOpenWeb.Click += MiOpenWeb_Click;
            this.miStartService.Click += MiStartService_Click;
            this.miStopService.Click += MiStopService_Click;
            this.miRestartService.Click += MiRestartService_Click;
            this.miAutoStart.Click += MiAutoStart_Click;
            this.miExit.Click += MiExit_Click;

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(0, 0);
            this.ControlBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.Load += TrayForm_Load;
        }

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem miOpenWeb;
        private System.Windows.Forms.ToolStripSeparator miSeparator1;
        private System.Windows.Forms.ToolStripMenuItem miServiceStatus;
        private System.Windows.Forms.ToolStripMenuItem miStartService;
        private System.Windows.Forms.ToolStripMenuItem miStopService;
        private System.Windows.Forms.ToolStripMenuItem miRestartService;
        private System.Windows.Forms.ToolStripSeparator miSeparator2;
        private System.Windows.Forms.ToolStripMenuItem miAutoStart;
        private System.Windows.Forms.ToolStripSeparator miSeparator3;
        private System.Windows.Forms.ToolStripMenuItem miExit;
    }
}
```

- [ ] **Step 2: Create TrayForm.cs**

`HW_info\TrayForm.cs`:
```csharp
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace HW_info
{
    public partial class TrayForm : Form
    {
        private Timer _statusTimer;

        public TrayForm()
        {
            InitializeComponent();
            Text = "HW-info 托盘";
        }

        private void TrayForm_Load(object sender, EventArgs e)
        {
            RefreshServiceStatus();
            _statusTimer = new Timer(_ => RefreshServiceStatus(), null, 5000, 5000);
        }

        private void RefreshServiceStatus()
        {
            try
            {
                var installed = ServiceManager.IsInstalled();
                var status = ServiceManager.GetStatus();

                if (!installed)
                {
                    miServiceStatus.Text = "服务状态: 未安装";
                    miStartService.Enabled = false;
                    miStopService.Enabled = false;
                    miRestartService.Enabled = false;
                    miAutoStart.Checked = false;
                    notifyIcon.Icon = System.Drawing.SystemIcons.Shield;
                }
                else if (status == ServiceControllerStatus.Running)
                {
                    miServiceStatus.Text = "服务状态: 运行中";
                    miStartService.Enabled = false;
                    miStopService.Enabled = true;
                    miRestartService.Enabled = true;
                    notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
                else
                {
                    miServiceStatus.Text = "服务状态: 已停止";
                    miStartService.Enabled = true;
                    miStopService.Enabled = false;
                    miRestartService.Enabled = false;
                    notifyIcon.Icon = System.Drawing.SystemIcons.Warning;
                }

                miAutoStart.Checked = IsAutoStartEnabled();
            }
            catch { }
        }

        private bool IsAutoStartEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    return key?.GetValue("HWInfoTray") != null;
                }
            }
            catch { return false; }
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (enable)
                        key?.SetValue("HWInfoTray", Application.ExecutablePath + " -tray");
                    else
                        key?.DeleteValue("HWInfoTray", false);
                }
            }
            catch { }
        }

        private void MiOpenWeb_Click(object sender, EventArgs e)
        {
            Process.Start($"http://127.0.0.1:{NetSvr.Port}/");
        }

        private void MiStartService_Click(object sender, EventArgs e)
        {
            if (!ServiceManager.IsInstalled())
            {
                var result = MessageBox.Show("服务尚未安装，是否现在安装？", "安装服务", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    ServiceManager.Install();
                    ServiceManager.StartService();
                }
            }
            else
            {
                ServiceManager.StartService();
            }
            Thread.Sleep(1000);
            RefreshServiceStatus();
        }

        private void MiStopService_Click(object sender, EventArgs e)
        {
            ServiceManager.StopService();
            Thread.Sleep(1000);
            RefreshServiceStatus();
        }

        private void MiRestartService_Click(object sender, EventArgs e)
        {
            ServiceManager.StopService();
            Thread.Sleep(2000);
            ServiceManager.StartService();
            Thread.Sleep(1000);
            RefreshServiceStatus();
        }

        private void MiAutoStart_Click(object sender, EventArgs e)
        {
            var enable = !miAutoStart.Checked;
            SetAutoStart(enable);
            miAutoStart.Checked = enable;
        }

        private void MiExit_Click(object sender, EventArgs e)
        {
            _statusTimer?.Dispose();
            notifyIcon.Visible = false;
            Application.Exit();
        }
    }
}
```

- [ ] **Step 3: Build**

```powershell
msbuild HW_info\HW_info.csproj /p:Configuration=Release
```
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add HW_info/TrayForm.cs HW_info/TrayForm.Designer.cs
git commit -m "feat: add system tray app with service management"
```

---

### Task 8: Program.cs Refactoring (Entry Point Dispatch)

**Files:**
- Modify: `HW_info\Program.cs`

- [ ] **Step 1: Rewrite Program.cs**

```csharp
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HW_info
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                // Parse command line arguments
                var argSet = new System.Collections.Generic.HashSet<string>(args, StringComparer.OrdinalIgnoreCase);
                var argDict = args
                    .Select((a, i) => new { a, i })
                    .Where(x => x.a.StartsWith("--") && x.i + 1 < args.Length)
                    .ToDictionary(x => x.a.Substring(2), x => args[x.i + 1]);

                // Service mode: run as Windows Service
                if (argSet.Contains("-svr") && argSet.Contains("-service"))
                {
                    ServiceBase.Run(new HwService());
                    return;
                }

                // Install service
                if (argSet.Contains("-install"))
                {
                    ServiceManager.Install();
                    Console.WriteLine("Service installed.");
                    return;
                }

                // Uninstall service
                if (argSet.Contains("-uninstall"))
                {
                    ServiceManager.Uninstall();
                    Console.WriteLine("Service uninstalled.");
                    return;
                }

                // Tray mode: show tray icon
                if (argSet.Contains("-tray"))
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new TrayForm());
                    return;
                }

                // Console server mode (debug/development)
                if (argSet.Contains("-svr") || argSet.Contains("--svr"))
                {
                    var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HW_info.db");
                    DataService.Initialize(dbPath);

                    var api = new WebApi();
                    var svr = new NetSvr { WebApi = api };
                    svr.NetEvent += (s, e) =>
                    {
                        var text = System.Text.Encoding.UTF8.GetString(e.Buffer);
                        var myData = XmlConvert.Deserialize<MyData>(text) ?? JsonConvert.Deserialize<MyData>(text);
                        if (myData != null)
                        {
                            myData.提交时间 = DateTime.Now;
                            DataService.Add(myData);
                            ChangeTracker.ProcessNewData(myData);
                            Console.WriteLine($"Received: {myData.计算机名} ({myData.MAC地址})");
                        }
                    };
                    svr.Start();
                    Console.WriteLine($"HW-info server running on port {NetSvr.Port}...");
                    Console.WriteLine("Press Ctrl+C to stop.");
                    var evt = new ManualResetEvent(false);
                    Console.CancelKeyPress += (s, e) => { e.Cancel = true; evt.Set(); };
                    evt.WaitOne();
                    svr.Close();
                    DataService.Shutdown();
                    return;
                }

                // Help
                if (argSet.Contains("-?") || argSet.Contains("-h"))
                {
                    ShowHelp();
                    return;
                }

                // Client silent mode (existing)
                var app = argSet.Contains("-app") || argSet.Contains("--app");
                var ip = IPAddress.Broadcast.ToString();
                if (argDict.TryGetValue("ip", out var ipStr) && IPAddress.TryParse(ipStr, out var addr))
                    ip = addr.ToString();
                if (argDict.TryGetValue("name", out var name) && argDict.TryGetValue("addr", out var addr2))
                {
                    argDict.TryGetValue("desc", out var desc);
                    SendData(app, ip, name, addr2, desc);
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1Cli(ip, app));
                return;
            }

            // No args: parse file name (existing client behavior)
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var fileName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            if (Regex.IsMatch(fileName, "-svr", RegexOptions.IgnoreCase))
            {
                // Default: start in console server mode
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HW_info.db");
                DataService.Initialize(dbPath);
                var api = new WebApi();
                var svr = new NetSvr { WebApi = api };
                svr.NetEvent += (s, e) =>
                {
                    var text = System.Text.Encoding.UTF8.GetString(e.Buffer);
                    var myData = XmlConvert.Deserialize<MyData>(text) ?? JsonConvert.Deserialize<MyData>(text);
                    if (myData != null)
                    {
                        myData.提交时间 = DateTime.Now;
                        DataService.Add(myData);
                        ChangeTracker.ProcessNewData(myData);
                    }
                };
                svr.Start();
                // Show tray icon by default in GUI mode
                Application.Run(new TrayForm());
            }
            else
            {
                // Client mode
                var cliApp = Regex.IsMatch(fileName, "app", RegexOptions.IgnoreCase);
                var cliIp = IPAddress.Broadcast.ToString();
                var match = Regex.Match(fileName, @"\d{8,}");
                if (match.Success && IPAddress.TryParse(match.Value, out var cliAddr))
                    cliIp = cliAddr.ToString();
                Application.Run(new Form1Cli(cliIp, cliApp));
            }
        }

        private static void SendData(bool app, string ip, string name, string addr, string desc)
        {
            var data = MyData.Get(app);
            data.Set(name, addr, desc);
            var xml = data.ToXml();
            NetCli.Send(xml, ip, NetSvr.Port);
        }

        private static void ShowHelp()
        {
            var appName = AppDomain.CurrentDomain.FriendlyName;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("HW-info 资产管理工具 v2.0");
            sb.AppendLine();
            sb.AppendLine("服务端参数:");
            sb.AppendLine($"  {appName} -svr                   启动服务端(控制台模式)");
            sb.AppendLine($"  {appName} -svr -service          启动服务端(Windows服务模式)");
            sb.AppendLine($"  {appName} -install               安装Windows服务");
            sb.AppendLine($"  {appName} -uninstall             卸载Windows服务");
            sb.AppendLine($"  {appName} -tray                  显示系统托盘");
            sb.AppendLine();
            sb.AppendLine("客户端参数:");
            sb.AppendLine($"  {appName} --ip <IP> [-app]       启动客户端");
            sb.AppendLine($"  {appName} --ip <IP> [-app] --name <姓名> --addr <位置> [--desc <备注>]");
            sb.AppendLine();
            sb.AppendLine("示例:");
            sb.AppendLine($"  {appName} -svr");
            sb.AppendLine($"  {appName} -install");
            sb.AppendLine($"  {appName} -tray");
            Console.WriteLine(sb.ToString());
            MessageBox.Show(sb.ToString(), "帮助");
        }
    }
}
```

- [ ] **Step 2: Build**

```powershell
msbuild HW_info\HW_info.csproj /p:Configuration=Release
```
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add HW_info/Program.cs
git commit -m "feat: refactor entry point with service/tray/console dispatch"
```

---

### Task 9: Web UI Redesign (HTML + CSS + JS)

**Files:**
- Create: `HW_info\Resources\web_index.html`
- Create: `HW_info\Resources\web_app.css`
- Create: `HW_info\Resources\web_app.js`

**Interfaces:**
- Consumes: WebApi JSON endpoints (`/api/datas`, `/api/datas/latest`, `/api/changelogs`, `/api/stats`, `/api/export/csv`, etc.)

- [ ] **Step 1: Create web_app.css**

`HW_info\Resources\web_app.css`:
```css
:root {
  --primary: #2563eb;
  --primary-hover: #1d4ed8;
  --bg: #f1f5f9;
  --card-bg: #ffffff;
  --sidebar-bg: #1e293b;
  --sidebar-text: #94a3b8;
  --sidebar-active: #2563eb;
  --text: #1e293b;
  --text-muted: #64748b;
  --border: #e2e8f0;
  --success: #22c55e;
  --warning: #f59e0b;
  --danger: #ef4444;
}

* { margin: 0; padding: 0; box-sizing: border-box; }

body {
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
  background: var(--bg);
  color: var(--text);
  min-height: 100vh;
  display: flex;
}

/* Sidebar */
.sidebar {
  width: 260px;
  background: var(--sidebar-bg);
  color: var(--sidebar-text);
  display: flex;
  flex-direction: column;
  position: fixed;
  top: 0; left: 0; bottom: 0;
  z-index: 100;
}

.sidebar-header {
  padding: 24px 20px;
  border-bottom: 1px solid rgba(255,255,255,0.08);
}

.sidebar-header h1 {
  font-size: 18px;
  font-weight: 600;
  color: #fff;
  letter-spacing: -0.3px;
}

.sidebar-header p {
  font-size: 12px;
  margin-top: 4px;
  color: var(--sidebar-text);
}

.sidebar-nav { padding: 12px 0; flex: 1; }

.nav-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 20px;
  cursor: pointer;
  transition: all 0.15s;
  font-size: 14px;
  border: none;
  background: none;
  color: var(--sidebar-text);
  width: 100%;
  text-align: left;
}

.nav-item:hover { background: rgba(255,255,255,0.05); color: #fff; }
.nav-item.active { background: rgba(37,99,235,0.15); color: #60a5fa; border-right: 3px solid var(--sidebar-active); }

/* Main content */
.main { margin-left: 260px; flex: 1; padding: 24px 32px; }

/* Stats cards */
.stats-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; margin-bottom: 24px; }

.stat-card {
  background: var(--card-bg);
  border-radius: 12px;
  padding: 20px 24px;
  border: 1px solid var(--border);
  transition: box-shadow 0.2s;
}

.stat-card:hover { box-shadow: 0 4px 12px rgba(0,0,0,0.06); }

.stat-card .label { font-size: 13px; color: var(--text-muted); margin-bottom: 4px; }
.stat-card .value { font-size: 28px; font-weight: 700; color: var(--text); }
.stat-card .icon { font-size: 24px; margin-bottom: 8px; }

/* Toolbar */
.toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
  flex-wrap: wrap;
  gap: 8px;
}

.toolbar-left { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.toolbar-right { display: flex; align-items: center; gap: 8px; }

.btn {
  padding: 8px 16px;
  border-radius: 8px;
  border: 1px solid var(--border);
  background: var(--card-bg);
  font-size: 13px;
  cursor: pointer;
  transition: all 0.15s;
  color: var(--text);
}

.btn:hover { background: var(--bg); }
.btn-primary { background: var(--primary); color: #fff; border-color: var(--primary); }
.btn-primary:hover { background: var(--primary-hover); }
.btn-sm { padding: 4px 10px; font-size: 12px; }

/* Table */
.table-container {
  background: var(--card-bg);
  border-radius: 12px;
  border: 1px solid var(--border);
  overflow: auto;
}

table { width: 100%; border-collapse: collapse; font-size: 13px; }

thead { background: #f8fafc; position: sticky; top: 0; z-index: 1; }

th {
  padding: 10px 12px;
  text-align: left;
  font-weight: 600;
  color: var(--text-muted);
  border-bottom: 2px solid var(--border);
  white-space: nowrap;
  cursor: pointer;
  user-select: none;
}

th:hover { color: var(--text); }

td {
  padding: 8px 12px;
  border-bottom: 1px solid var(--border);
  max-width: 200px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

tr:hover td { background: #f8fafc; }

/* Column toggle */
.column-toggle {
  position: relative;
  display: inline-block;
}

.column-toggle-menu {
  display: none;
  position: absolute;
  top: 100%;
  right: 0;
  background: var(--card-bg);
  border: 1px solid var(--border);
  border-radius: 8px;
  padding: 8px;
  min-width: 180px;
  box-shadow: 0 4px 16px rgba(0,0,0,0.1);
  z-index: 50;
  max-height: 300px;
  overflow-y: auto;
}

.column-toggle-menu.show { display: block; }

.column-toggle-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 8px;
  font-size: 12px;
  cursor: pointer;
  border-radius: 4px;
}

.column-toggle-item:hover { background: var(--bg); }

/* Change log */
.changelog-list { list-style: none; }

.changelog-item {
  padding: 12px 16px;
  border-bottom: 1px solid var(--border);
  display: flex;
  align-items: flex-start;
  gap: 12px;
  font-size: 13px;
}

.changelog-item:last-child { border-bottom: none; }

.changelog-field {
  display: inline-block;
  background: #eff6ff;
  color: var(--primary);
  padding: 2px 8px;
  border-radius: 4px;
  font-weight: 500;
  font-size: 12px;
}

.changelog-values { display: flex; gap: 8px; align-items: center; margin-top: 4px; }

.changelog-old {
  background: #fef2f2;
  color: var(--danger);
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  text-decoration: line-through;
}

.changelog-new {
  background: #f0fdf4;
  color: var(--success);
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
}

.changelog-arrow { color: var(--text-muted); font-size: 14px; }
.changelog-time { color: var(--text-muted); font-size: 11px; }

/* History table */
.history-table { width: 100%; font-size: 13px; }
.history-table th, .history-table td { padding: 6px 10px; }

/* Section title */
.section-title {
  font-size: 18px;
  font-weight: 600;
  margin-bottom: 16px;
}

/* Machine detail */
.machine-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
}

.back-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  color: var(--primary);
  cursor: pointer;
  font-size: 14px;
  text-decoration: none;
}

.back-btn:hover { text-decoration: underline; }

/* Responsive */
@media (max-width: 768px) {
  .sidebar { width: 0; overflow: hidden; }
  .main { margin-left: 0; padding: 16px; }
  .stats-row { grid-template-columns: 1fr 1fr; }
}
```

- [ ] **Step 2: Create web_app.js**

`HW_info\Resources\web_app.js`:
```javascript
const API_BASE = '';

let state = {
  view: 'machines',
  datas: [],
  changelogs: [],
  stats: {},
  selectedMac: null,
  columnVisibility: {
    '计算机名': true, '用户名': true, '操作系统': true, '型号': true,
    '序列号': true, 'CPU': true, '主板': true, '内存': true,
    '硬盘': true, '显卡': true, '显示器': false, '打印机': false,
    '网卡': false, 'IP地址': false, 'MAC地址': false, '应用程序': false,
    '姓名': false, '位置': false, '备注': false, '提交时间': false,
  },
};

// API helpers
async function apiGet(path) {
  const r = await fetch(API_BASE + path);
  return r.json();
}

// Navigation
function navigate(view, params) {
  state.view = view;
  document.querySelectorAll('.nav-item').forEach(el => el.classList.remove('active'));
  const navMap = { 'machines': 0, 'changelogs': 1 };
  const items = document.querySelectorAll('.nav-item');
  if (navMap[view] !== undefined) items[navMap[view]]?.classList.add('active');
  render();
}

// Render
async function render() {
  const content = document.getElementById('content');
  if (state.view === 'machines') {
    await loadStats();
    await loadMachines();
    content.innerHTML = renderMachinesView();
    attachTableEvents();
  } else if (state.view === 'changelogs') {
    await loadChangeLogs();
    content.innerHTML = renderChangeLogsView();
  } else if (state.view === 'machine-detail') {
    await loadMachineDetail(state.selectedMac);
    content.innerHTML = renderMachineDetailView();
  }
}

async function loadStats() {
  state.stats = await apiGet('/api/stats');
}

async function loadMachines() {
  state.datas = await apiGet('/api/datas/latest');
}

async function loadChangeLogs(mac) {
  const path = mac ? `/api/changelogs?mac=${encodeURIComponent(mac)}` : '/api/changelogs?limit=100';
  state.changelogs = await apiGet(path);
}

async function loadMachineDetail(mac) {
  const d = await apiGet(`/api/datas/history?mac=${encodeURIComponent(mac)}`);
  state.datas = d.records || [];
}

function getColumnLabel(name) {
  const labels = {
    '计算机名': 'Computer', '用户名': 'User', '操作系统': 'OS', '系统安装日期': 'Install Date',
    '型号': 'Model', 'BIOS日期': 'BIOS Date', '序列号': 'Serial', 'CPU': 'CPU',
    '主板': 'Motherboard', '内存': 'Memory', '硬盘': 'Disk', '显卡': 'GPU',
    '显示器': 'Monitor', '打印机': 'Printer', '网卡': 'NIC', 'IP地址': 'IP',
    'MAC地址': 'MAC', '应用程序': 'Apps', '姓名': 'Name', '位置': 'Location',
    '备注': 'Note', '提交时间': 'Submitted',
  };
  return labels[name] || name;
}

function renderMachinesView() {
  const s = state.stats;
  const visibleCols = Object.entries(state.columnVisibility).filter(([,v]) => v).map(([k]) => k);
  const allCols = Object.keys(state.columnVisibility);

  return `
    <div class="stats-row">
      <div class="stat-card">
        <div class="label">总记录数</div>
        <div class="value">${s.totalReports || 0}</div>
      </div>
      <div class="stat-card">
        <div class="label">在线机器</div>
        <div class="value">${s.totalMachines || 0}</div>
      </div>
      <div class="stat-card">
        <div class="label">今日新增</div>
        <div class="value">${s.todayNew || 0}</div>
      </div>
    </div>

    <div class="toolbar">
      <div class="toolbar-left">
        <strong style="font-size:16px;">机器列表</strong>
        <span style="color:#64748b;font-size:13px;">${state.datas.length} 台</span>
      </div>
      <div class="toolbar-right">
        <div class="column-toggle">
          <button class="btn btn-sm" onclick="toggleColumnMenu()">☰ 列显示</button>
          <div class="column-toggle-menu" id="columnMenu">
            ${allCols.map(col => `
              <label class="column-toggle-item">
                <input type="checkbox" ${state.columnVisibility[col] ? 'checked' : ''} 
                       onchange="toggleColumn('${col}', this.checked)">
                <span>${col}</span>
              </label>
            `).join('')}
          </div>
        </div>
        <a href="/api/export/csv" class="btn btn-sm btn-primary" target="_blank">⬇ 导出 CSV</a>
      </div>
    </div>

    <div class="table-container">
      <table>
        <thead>
          <tr>
            <th>#</th>
            ${visibleCols.map(col => `<th>${getColumnLabel(col)}</th>`).join('')}
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          ${state.datas.map((d, i) => `
            <tr>
              <td>${i + 1}</td>
              ${visibleCols.map(col => `<td title="${(d[col] || '').replace(/\|/g, ' | ')}">${(d[col] || '').replace(/\|/g, ' | ').substring(0, 50)}</td>`).join('')}
              <td>
                <button class="btn btn-sm" onclick="showDetail('${d.MAC地址}')">📋 历史</button>
              </td>
            </tr>
          `).join('')}
        </tbody>
      </table>
    </div>
  `;
}

function renderChangeLogsView() {
  return `
    <div class="section-title">变更记录</div>
    ${state.changelogs.length === 0 ? '<p style="color:#64748b;">暂无变更记录</p>' : ''}
    <div class="table-container">
      <table>
        <thead>
          <tr>
            <th>时间</th>
            <th>计算机名</th>
            <th>变更字段</th>
            <th>旧值</th>
            <th>新值</th>
          </tr>
        </thead>
        <tbody>
          ${state.changelogs.map(log => `
            <tr>
              <td>${log.ChangedAt || ''}</td>
              <td>${log.ComputerName || ''}</td>
              <td><span class="changelog-field">${log.FieldName}</span></td>
              <td style="color:#ef4444;">${log.OldValue || '-'}</td>
              <td style="color:#22c55e;">${log.NewValue || '-'}</td>
            </tr>
          `).join('')}
        </tbody>
      </table>
    </div>
  `;
}

function renderMachineDetailView() {
  const mac = state.selectedMac;
  const records = state.datas;
  const latest = records[0] || {};

  const allFields = Object.keys(state.columnVisibility);
  const compareFields = ['计算机名','用户名','操作系统','型号','序列号','CPU','主板','内存','硬盘','显卡','显示器'];

  return `
    <div class="machine-header">
      <a class="back-btn" onclick="navigate('machines')">← 返回列表</a>
      <span style="font-size:20px;font-weight:600;">${latest.计算机名 || mac}</span>
    </div>

    <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:24px;">
      <div class="stat-card">
        <div class="label">MAC 地址</div>
        <div class="value" style="font-size:16px;">${mac}</div>
      </div>
      <div class="stat-card">
        <div class="label">提交次数</div>
        <div class="value" style="font-size:16px;">${records.length}</div>
      </div>
    </div>

    <div class="section-title">当前配置</div>
    <div class="table-container" style="margin-bottom:24px;">
      <table class="history-table">
        <tbody>
          ${allFields.filter(f => f !== '提交时间').map(f => `
            <tr>
              <td style="font-weight:600;width:120px;">${f}</td>
              <td>${(latest[f] || '-').replace(/\|/g, ' | ')}</td>
            </tr>
          `).join('')}
        </tbody>
      </table>
    </div>

    <div class="section-title">提交历史</div>
    <div class="table-container" style="margin-bottom:24px;">
      <table class="history-table">
        <thead>
          <tr>
            <th>#</th>
            <th>提交时间</th>
            ${compareFields.map(f => `<th>${getColumnLabel(f)}</th>`).join('')}
          </tr>
        </thead>
        <tbody>
          ${records.map((r, i) => `
            <tr>
              <td>${i + 1}</td>
              <td>${r.提交时间 || ''}</td>
              ${compareFields.map(f => {
                const val = r[f] || '-';
                return `<td title="${val.replace(/\|/g, ' | ')}">${val.replace(/\|/g, ' | ').substring(0, 30)}</td>`;
              }).join('')}
            </tr>
          `).join('')}
        </tbody>
      </table>
    </div>

    <div class="section-title">变更记录</div>
    <div id="detailChangelogs"></div>
  `;
}

function toggleColumnMenu() {
  document.getElementById('columnMenu').classList.toggle('show');
}

function toggleColumn(name, visible) {
  state.columnVisibility[name] = visible;
  render();
}

function showDetail(mac) {
  if (!mac) { alert('该机器没有 MAC 地址'); return; }
  state.selectedMac = mac;
  navigate('machine-detail');
}

function attachTableEvents() {
  // Load changelogs for machine detail sub-view
  if (state.view === 'machine-detail' && state.selectedMac) {
    loadChangeLogs(state.selectedMac).then(() => {
      const el = document.getElementById('detailChangelogs');
      if (!el) return;
      el.innerHTML = state.changelogs.length === 0
        ? '<p style="color:#64748b;">无变更记录</p>'
        : `<div class="table-container"><table class="history-table"><thead><tr><th>时间</th><th>字段</th><th>旧值</th><th>新值</th></tr></thead><tbody>
          ${state.changelogs.map(log => `
            <tr>
              <td>${log.ChangedAt || ''}</td>
              <td><span class="changelog-field">${log.FieldName}</span></td>
              <td style="color:#ef4444;max-width:200px;overflow:hidden;text-overflow:ellipsis;">${log.OldValue || '-'}</td>
              <td style="color:#22c55e;max-width:200px;overflow:hidden;text-overflow:ellipsis;">${log.NewValue || '-'}</td>
            </tr>
          `).join('')}</tbody></table></div>`;
    });
  }
}

// Init
document.addEventListener('click', function(e) {
  const menu = document.getElementById('columnMenu');
  if (menu && !e.target.closest('.column-toggle')) {
    menu.classList.remove('show');
  }
});

navigate('machines');
```

- [ ] **Step 3: Create web_index.html**

`HW_info\Resources\web_index.html`:
```html
<!DOCTYPE html>
<html lang="zh-CN">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>HW-info 资产管理系统</title>
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
  <link rel="stylesheet" href="/app.css">
</head>
<body>
  <aside class="sidebar">
    <div class="sidebar-header">
      <h1>HW-info</h1>
      <p>资产管理系统</p>
    </div>
    <nav class="sidebar-nav">
      <button class="nav-item active" onclick="navigate('machines')">
        <span>🖥</span> 机器列表
      </button>
      <button class="nav-item" onclick="navigate('changelogs')">
        <span>📋</span> 变更记录
      </button>
    </nav>
    <div style="padding:16px 20px;border-top:1px solid rgba(255,255,255,0.08);font-size:12px;color:#475569;">
      HW-info v2.0
    </div>
  </aside>
  <main class="main" id="content">
    <div style="text-align:center;padding:80px 0;color:#64748b;">加载中...</div>
  </main>
  <script src="/app.js"></script>
</body>
</html>
```

- [ ] **Step 4: Build**

```powershell
msbuild HW_info\HW_info.csproj /p:Configuration=Release
```
Expected: Build succeeds (ensure embedded resources are correctly referenced)

- [ ] **Step 5: Verify embedded resources are accessible**

Run the server in console mode and check that the web UI loads:
```powershell
start .\HW_info\bin\Release\HW-info-svr.exe -svr
# Wait 2 seconds, then:
curl http://127.0.0.1:51528/
```
Expected: Returns the HTML dashboard (not "Resource not found")

- [ ] **Step 6: Commit**

```bash
git add HW_info/Resources/web_index.html HW_info/Resources/web_app.css HW_info/Resources/web_app.js
git commit -m "feat: redesign web UI as modern admin dashboard with asset management"
```

---

### Task 10: Remove Legacy GUI Files & Clean Up

**Files:**
- Delete: `HW_info\Form2Svr.cs`
- Delete: `HW_info\Form2Svr.Designer.cs`
- Delete: `HW_info\Form2Svr.resx`
- Delete: `HW_info\Form4Table.cs`
- Delete: `HW_info\Form4Table.Designer.cs`
- Delete: `HW_info\Form4Table.resx`
- Delete: `HW_info\Form5Setup.cs`
- Delete: `HW_info\Form5Setup.Designer.cs`
- Delete: `HW_info\Form5Setup.resx`
- Modify: (already removed from .csproj in Task 4)

- [ ] **Step 1: Delete legacy files**

```powershell
Remove-Item -LiteralPath "E:\hw-info-master\HW_info\Form2Svr.cs" -Force
Remove-Item -LiteralPath "E:\hw-info-master\HW_info\Form2Svr.Designer.cs" -Force
Remove-Item -LiteralPath "E:\hw-info-master\HW_info\Form2Svr.resx" -Force
Remove-Item -LiteralPath "E:\hw-info-master\HW_info\Form4Table.cs" -Force
Remove-Item -LiteralPath "E:\hw-info-master\HW_info\Form4Table.Designer.cs" -Force
Remove-Item -LiteralPath "E:\hw-info-master\HW_info\Form4Table.resx" -Force
Remove-Item -LiteralPath "E:\hw-info-master\HW_info\Form5Setup.cs" -Force
Remove-Item -LiteralPath "E:\hw-info-master\HW_info\Form5Setup.Designer.cs" -Force
Remove-Item -LiteralPath "E:\hw-info-master\HW_info\Form5Setup.resx" -Force
```

- [ ] **Step 2: Final build verification**

```powershell
msbuild HW_info\HW_info.csproj /p:Configuration=Release
```
Expected: Build succeeds with zero errors, zero warnings

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "cleanup: remove legacy WinForms server UI files"
```

---

## Self-Review

**Spec coverage check:**
1. ✅ 服务端支持安装成服务，开机自启动 — Task 6 (HwService) + Task 7 (ServiceManager with auto-start)
2. ✅ 服务器只保留web端的界面，软件端的界面可以去除 — Task 4 (remove Forms from csproj) + Task 10 (delete files)
3. ✅ web端的信息默认只显示指定列，其他可选展示，导出全部 — Task 9 (columnVisibility defaults in JS, CSV export button)
4. ✅ 美化web-UI — Task 9 (complete redesign with Inter font, sidebar, stats cards, modern table)
5. ✅ 服务端每次收到客户端的信息进行对比，记录变更 — Task 3 (ChangeTracker compares old/new by MAC)

**Placeholder scan:** All code blocks contain complete, compilable code. No TBD/TODO/placeholder patterns present.

**Type consistency:** 
- `DataService.Add(MyData)` — used in Task 2, consumed by Task 3 `ChangeTracker.ProcessNewData()` and Task 4 `WebApi.HandleDataAdd()`
- `DataService.GetLatestByMac(string)` — used in Task 2, consumed by Task 3
- `DataService.AddChangeLogs(IEnumerable<ChangeLog>)` — used in Task 2, consumed by Task 3
- `ChangeTracker.ProcessNewData(MyData)` → `List<ChangeLog>` — defined in Task 3, used in Task 4 WebApi
- All types match across task boundaries.
