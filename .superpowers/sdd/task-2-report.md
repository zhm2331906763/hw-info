# Task 2 Report: DataService + ChangeLog + Tests

## Files Created

| File | Description |
|------|-------------|
| `HW_info\ChangeLog.cs` | Model class for change tracking records (Id, MacAddress, ComputerName, FieldName, OldValue, NewValue, ChangedAt) |
| `HW_info\DataService.cs` | Static SQLite data access layer with full CRUD for MachineReports and ChangeLogs |
| `Tests\DataServiceTests.cs` | 5 xUnit tests replacing placeholder |

## Project Changes

- `HW_info\HW_info.csproj` — Added `<Compile Include="ChangeLog.cs" />` and `<Compile Include="DataService.cs" />` to explicit compile list

## DataService API

| Method | Description |
|--------|-------------|
| `Initialize(string dbPath)` | Opens SQLite connection, creates tables + indexes |
| `Shutdown()` | Closes connection |
| `Add(MyData)` | Insert machine report |
| `AddChangeLog(ChangeLog)` | Insert single change log |
| `AddChangeLogs(IEnumerable<ChangeLog>)` | Batch insert with transaction |
| `GetAll()` | All reports ordered by 提交时间 DESC |
| `GetLatestByMac(string)` | Most recent report for a MAC |
| `GetLatest()` | One row per MAC (latest by Id) |
| `GetByMac(string)` | All history for a MAC |
| `GetChangeLogs(string, int)` | Changelogs optionally filtered by MAC |
| `ExportCsv()` | Full CSV string with all 24 columns |
| `GetTotalCount()` | Total report count |
| `GetDistinctMacCount()` | Distinct MAC count |
| `GetTodayCount()` | Reports submitted today via `substr(提交时间,1,10)=date('now','localtime')` |

## Test Results

```
已通过 HW_info.Tests.DataServiceTests.AddAndRetrieveRecord [148 ms]
已通过 HW_info.Tests.DataServiceTests.GetLatest_ReturnsOnePerMac [71 ms]
已通过 HW_info.Tests.DataServiceTests.GetLatestByMac_ReturnsMostRecent [61 ms]
已通过 HW_info.Tests.DataServiceTests.ExportCsv_IncludesAllFields [50 ms]
已通过 HW_info.Tests.DataServiceTests.GetByMac_ReturnsAllRecords [57 ms]
已通过 HW_info.Tests.ChangeTrackerTests.Placeholder [8 ms]
```

**Total: 6 passed, 0 failed — SUCCESS**
