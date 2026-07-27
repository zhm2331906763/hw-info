# Task 3 Report: ChangeTracker

## Files Created/Modified

- **Created**: `HW_info\ChangeTracker.cs` — static class with `GetChanges()` (compares two MyData objects, yielding ChangeLogs for non-ignored, changed fields) and `ProcessNewData()` (loads old data from DB, computes deltas, persists them)
- **Modified**: `Tests\ChangeTrackerTests.cs` — replaced placeholder with 4 real tests
- **Modified**: `HW_info\HW_info.csproj` — added `<Compile Include="ChangeTracker.cs" />`

## Tests

| Test | Status |
|------|--------|
| `GetChanges_DetectsChangedFields` — CPU & memory changed, disk unchanged | PASS |
| `GetChanges_NoChanges_ReturnsEmpty` — same object compared | PASS |
| `GetChanges_IgnoresSpecifiedFields` — 姓名/位置/备注/提交时间 ignored | PASS |
| `GetChanges_NullOldData_ReturnsEmpty` — null old returns empty | PASS |

All 4 new tests pass. All 5 existing `DataServiceTests` also pass (total: 9/9).
