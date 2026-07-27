using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ClosedXML.Excel;

namespace HW_info
{
    public static class DataService
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetDllDirectory(string lpPathName);

        private static SQLiteConnection _connection;

        private static void ExtractNativeDll()
        {
            var is64 = IntPtr.Size == 8;
            var archDir = is64 ? "x64" : "x86";
            var dllName = "SQLite.Interop.dll";

            var targetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, archDir);
            var targetPath = Path.Combine(targetDir, dllName);

            if (File.Exists(targetPath)) return;

            Directory.CreateDirectory(targetDir);

            //从嵌入资源提取（构建时嵌入的原生DLL）
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"{archDir}.{dllName}";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                        stream.CopyTo(fs);
                    return;
                }
            }

            //备选：从 exe 同目录的 x64/x86 子目录复制
            var srcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, archDir, dllName);
            if (File.Exists(srcPath))
                File.Copy(srcPath, targetPath, true);
        }

        public static void Initialize(string dbPath = null)
        {
            //先提取原生SQLite DLL，否则会报 DllNotFoundException
            ExtractNativeDll();
            SetDllDirectory(AppDomain.CurrentDomain.BaseDirectory);

            if (string.IsNullOrEmpty(dbPath))
                dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HW_info.db");

            _connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            _connection.Open();

            using (var cmd = new SQLiteCommand(_connection))
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS MachineReports (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        MacAddress TEXT NOT NULL DEFAULT '',
                        计算机名 TEXT, 用户名 TEXT, 操作系统 TEXT, 系统安装日期 TEXT,
                        型号 TEXT, BIOS日期 TEXT, 序列号 TEXT, CPU TEXT,
                        主板 TEXT, 内存 TEXT, 硬盘 TEXT, 显卡 TEXT,
                        显示器 TEXT, 打印机 TEXT, 网卡 TEXT, IP地址 TEXT,
                        MAC地址 TEXT, 应用程序 TEXT, 姓名 TEXT, 位置 TEXT,
                        备注 TEXT, 提交时间 TEXT NOT NULL
                    )";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ChangeLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        MacAddress TEXT NOT NULL DEFAULT '',
                        ComputerName TEXT, FieldName TEXT NOT NULL,
                        OldValue TEXT, NewValue TEXT, ChangedAt TEXT NOT NULL
                    )";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE INDEX IF NOT EXISTS IX_MachineReports_MacAddress ON MachineReports(MacAddress)";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE INDEX IF NOT EXISTS IX_MachineReports_提交时间 ON MachineReports(提交时间)";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE INDEX IF NOT EXISTS IX_ChangeLogs_MacAddress ON ChangeLogs(MacAddress)";
                cmd.ExecuteNonQuery();
            }
        }

        public static void Shutdown()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }

        public static void Add(MyData data)
        {
            using (var cmd = new SQLiteCommand(_connection))
            {
                cmd.CommandText = @"
                    INSERT INTO MachineReports (MacAddress, 计算机名, 用户名, 操作系统, 系统安装日期, 型号, BIOS日期, 序列号, CPU, 主板, 内存, 硬盘, 显卡, 显示器, 打印机, 网卡, IP地址, MAC地址, 应用程序, 姓名, 位置, 备注, 提交时间)
                    VALUES (@MacAddress, @计算机名, @用户名, @操作系统, @系统安装日期, @型号, @BIOS日期, @序列号, @CPU, @主板, @内存, @硬盘, @显卡, @显示器, @打印机, @网卡, @IP地址, @MAC地址, @应用程序, @姓名, @位置, @备注, @提交时间)";

                cmd.Parameters.AddWithValue("@MacAddress", data.MAC地址 ?? "");
                cmd.Parameters.AddWithValue("@计算机名", data.计算机名 ?? "");
                cmd.Parameters.AddWithValue("@用户名", data.用户名 ?? "");
                cmd.Parameters.AddWithValue("@操作系统", data.操作系统 ?? "");
                cmd.Parameters.AddWithValue("@系统安装日期", data.系统安装日期 ?? "");
                cmd.Parameters.AddWithValue("@型号", data.型号 ?? "");
                cmd.Parameters.AddWithValue("@BIOS日期", data.BIOS日期 ?? "");
                cmd.Parameters.AddWithValue("@序列号", data.序列号 ?? "");
                cmd.Parameters.AddWithValue("@CPU", data.CPU ?? "");
                cmd.Parameters.AddWithValue("@主板", data.主板 ?? "");
                cmd.Parameters.AddWithValue("@内存", data.内存 ?? "");
                cmd.Parameters.AddWithValue("@硬盘", data.硬盘 ?? "");
                cmd.Parameters.AddWithValue("@显卡", data.显卡 ?? "");
                cmd.Parameters.AddWithValue("@显示器", data.显示器 ?? "");
                cmd.Parameters.AddWithValue("@打印机", data.打印机 ?? "");
                cmd.Parameters.AddWithValue("@网卡", data.网卡 ?? "");
                cmd.Parameters.AddWithValue("@IP地址", data.IP地址 ?? "");
                cmd.Parameters.AddWithValue("@MAC地址", data.MAC地址 ?? "");
                cmd.Parameters.AddWithValue("@应用程序", data.应用程序 ?? "");
                cmd.Parameters.AddWithValue("@姓名", data.姓名 ?? "");
                cmd.Parameters.AddWithValue("@位置", data.位置 ?? "");
                cmd.Parameters.AddWithValue("@备注", data.备注 ?? "");
                cmd.Parameters.AddWithValue("@提交时间", data.提交时间.ToString("yyyy-MM-dd HH:mm:ss"));

                cmd.ExecuteNonQuery();
            }
        }

        public static void AddChangeLog(ChangeLog log)
        {
            using (var cmd = new SQLiteCommand(_connection))
            {
                cmd.CommandText = @"
                    INSERT INTO ChangeLogs (MacAddress, ComputerName, FieldName, OldValue, NewValue, ChangedAt)
                    VALUES (@MacAddress, @ComputerName, @FieldName, @OldValue, @NewValue, @ChangedAt)";

                cmd.Parameters.AddWithValue("@MacAddress", log.MacAddress ?? "");
                cmd.Parameters.AddWithValue("@ComputerName", log.ComputerName ?? "");
                cmd.Parameters.AddWithValue("@FieldName", log.FieldName);
                cmd.Parameters.AddWithValue("@OldValue", log.OldValue ?? "");
                cmd.Parameters.AddWithValue("@NewValue", log.NewValue ?? "");
                cmd.Parameters.AddWithValue("@ChangedAt", log.ChangedAt.ToString("yyyy-MM-dd HH:mm:ss"));

                cmd.ExecuteNonQuery();
            }
        }

        public static void AddChangeLogs(IEnumerable<ChangeLog> logs)
        {
            using (var tx = _connection.BeginTransaction())
            using (var cmd = new SQLiteCommand(_connection))
            {
                cmd.CommandText = @"
                    INSERT INTO ChangeLogs (MacAddress, ComputerName, FieldName, OldValue, NewValue, ChangedAt)
                    VALUES (@MacAddress, @ComputerName, @FieldName, @OldValue, @NewValue, @ChangedAt)";

                foreach (var log in logs)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@MacAddress", log.MacAddress ?? "");
                    cmd.Parameters.AddWithValue("@ComputerName", log.ComputerName ?? "");
                    cmd.Parameters.AddWithValue("@FieldName", log.FieldName);
                    cmd.Parameters.AddWithValue("@OldValue", log.OldValue ?? "");
                    cmd.Parameters.AddWithValue("@NewValue", log.NewValue ?? "");
                    cmd.Parameters.AddWithValue("@ChangedAt", log.ChangedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
        }

        public static List<MyData> GetAll()
        {
            var result = new List<MyData>();
            using (var cmd = new SQLiteCommand("SELECT * FROM MachineReports ORDER BY 提交时间 DESC", _connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    result.Add(ReadMyData(reader));
            }
            return result;
        }

        public static MyData GetLatestByMac(string macAddress)
        {
            using (var cmd = new SQLiteCommand(_connection))
            {
                cmd.CommandText = "SELECT * FROM MachineReports WHERE MacAddress = @MacAddress ORDER BY 提交时间 DESC LIMIT 1";
                cmd.Parameters.AddWithValue("@MacAddress", macAddress);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return ReadMyData(reader);
                }
            }
            return null;
        }

        public static List<MyData> GetLatest()
        {
            var result = new List<MyData>();
            using (var cmd = new SQLiteCommand(_connection))
            {
                cmd.CommandText = @"
                    SELECT * FROM MachineReports 
                    WHERE Id IN (SELECT MAX(Id) FROM MachineReports GROUP BY MacAddress)
                    ORDER BY 提交时间 DESC";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        result.Add(ReadMyData(reader));
                }
            }
            return result;
        }

        public static List<MyData> GetByMac(string macAddress)
        {
            var result = new List<MyData>();
            using (var cmd = new SQLiteCommand(_connection))
            {
                cmd.CommandText = "SELECT * FROM MachineReports WHERE MacAddress = @MacAddress ORDER BY 提交时间 DESC";
                cmd.Parameters.AddWithValue("@MacAddress", macAddress);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        result.Add(ReadMyData(reader));
                }
            }
            return result;
        }

        public static List<ChangeLog> GetChangeLogs(string macAddress = null, int limit = 200)
        {
            var result = new List<ChangeLog>();
            using (var cmd = new SQLiteCommand(_connection))
            {
                if (string.IsNullOrEmpty(macAddress))
                {
                    cmd.CommandText = "SELECT * FROM ChangeLogs ORDER BY ChangedAt DESC LIMIT @Limit";
                    cmd.Parameters.AddWithValue("@Limit", limit);
                }
                else
                {
                    cmd.CommandText = "SELECT * FROM ChangeLogs WHERE MacAddress = @MacAddress ORDER BY ChangedAt DESC LIMIT @Limit";
                    cmd.Parameters.AddWithValue("@MacAddress", macAddress);
                    cmd.Parameters.AddWithValue("@Limit", limit);
                }
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ChangeLog
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            MacAddress = reader["MacAddress"] as string ?? "",
                            ComputerName = reader["ComputerName"] as string ?? "",
                            FieldName = reader["FieldName"] as string ?? "",
                            OldValue = reader["OldValue"] as string ?? "",
                            NewValue = reader["NewValue"] as string ?? "",
                            ChangedAt = DateTime.Parse(reader["ChangedAt"].ToString())
                        });
                    }
                }
            }
            return result;
        }

        public static string ExportCsv()
        {
            var cols = new[] {
                "计算机名","用户名","操作系统","系统安装日期","型号","BIOS日期","序列号",
                "CPU数量","CPU","主板","内存数量","内存","硬盘数量","硬盘",
                "显卡","显示器数量","显示器","打印机","网卡","IP地址","MAC地址",
                "应用程序","姓名","位置","备注","提交时间"
            };
            var countFields = new Dictionary<string, string> {
                { "CPU", "CPU数量" }, { "内存", "内存数量" }, { "硬盘", "硬盘数量" }, { "显示器", "显示器数量" }
            };
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", cols.Select(c => EscapeCsv(c))));

            using (var cmd = new SQLiteCommand(@"
                SELECT 计算机名,用户名,操作系统,系统安装日期,型号,BIOS日期,序列号,CPU,主板,内存,硬盘,显卡,显示器,打印机,网卡,IP地址,MAC地址,应用程序,姓名,位置,备注,提交时间
                FROM MachineReports
                WHERE Id IN (SELECT MAX(Id) FROM MachineReports GROUP BY MacAddress)
                ORDER BY 提交时间 DESC", _connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var values = new Dictionary<string, string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        values[reader.GetName(i)] = reader[i]?.ToString() ?? "";

                    var row = new List<string>();
                    foreach (var col in cols)
                    {
                        if (countFields.ContainsValue(col))
                        {
                            var srcCol = countFields.First(kv => kv.Value == col).Key;
                            var cnt = values.ContainsKey(srcCol) ? values[srcCol].Split('|').Count(s => s.Trim().Length > 0) : 0;
                            row.Add(cnt.ToString());
                        }
                        else
                        {
                            row.Add(values.ContainsKey(col) ? values[col] : "");
                        }
                    }
                    sb.AppendLine(string.Join(",", row.Select(v => EscapeCsv(v))));
                }
            }
            return sb.ToString();
        }

        public static byte[] ExportXlsx()
        {
            var columns = new[] {
                "计算机名","用户名","操作系统","系统安装日期","型号","BIOS日期","序列号",
                "CPU数量","CPU","主板","内存数量","内存","硬盘数量","硬盘",
                "显卡","显示器数量","显示器","打印机","网卡","IP地址","MAC地址",
                "应用程序","姓名","位置","备注","提交时间"
            };
            var countSrc = new Dictionary<string, string> {
                { "CPU数量", "CPU" }, { "内存数量", "内存" }, { "硬盘数量", "硬盘" }, { "显示器数量", "显示器" }
            };

            var dt = new DataTable();
            foreach (var c in columns) dt.Columns.Add(c);

            using (var cmd = new SQLiteCommand(@"
                SELECT 计算机名,用户名,操作系统,系统安装日期,型号,BIOS日期,序列号,CPU,主板,内存,硬盘,显卡,显示器,打印机,网卡,IP地址,MAC地址,应用程序,姓名,位置,备注,提交时间
                FROM MachineReports
                WHERE Id IN (SELECT MAX(Id) FROM MachineReports GROUP BY MacAddress)
                ORDER BY 提交时间 DESC", _connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var raw = new Dictionary<string, string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        raw[reader.GetName(i)] = reader[i]?.ToString() ?? "";

                    var row = dt.NewRow();
                    for (int ci = 0; ci < columns.Length; ci++)
                    {
                        var col = columns[ci];
                        if (countSrc.TryGetValue(col, out var srcCol))
                        {
                            raw.TryGetValue(srcCol, out var srcVal);
                            var items = (srcVal ?? "").Split('|').Where(s => s.Trim().Length > 0).ToArray();
                            row[ci] = items.Length;
                        }
                        else
                        {
                            raw.TryGetValue(col, out var val);
                            row[ci] = (val ?? "").Replace("|", "\n");
                        }
                    }
                    dt.Rows.Add(row);
                }
            }

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("硬件资产信息");
                ws.Cell(1, 1).InsertTable(dt);

                //设置自动换行
                ws.Rows().Style.Alignment.WrapText = true;

                //自动调整列宽
                ws.Columns().AdjustToContents(1, 80);

                //表头样式
                var headerRow = ws.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
                headerRow.Style.Font.FontColor = XLColor.White;
                headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                using (var ms = new MemoryStream())
                {
                    wb.SaveAs(ms);
                    return ms.ToArray();
                }
            }
        }

        public static int GetTotalCount()
        {
            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM MachineReports", _connection))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static int GetDistinctMacCount()
        {
            using (var cmd = new SQLiteCommand("SELECT COUNT(DISTINCT MacAddress) FROM MachineReports", _connection))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static int GetTodayCount()
        {
            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM MachineReports WHERE substr(提交时间, 1, 10) = date('now', 'localtime')", _connection))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static MyData ReadMyData(SQLiteDataReader reader)
        {
            return new MyData
            {
                计算机名 = reader["计算机名"] as string ?? "",
                用户名 = reader["用户名"] as string ?? "",
                操作系统 = reader["操作系统"] as string ?? "",
                系统安装日期 = reader["系统安装日期"] as string ?? "",
                型号 = reader["型号"] as string ?? "",
                BIOS日期 = reader["BIOS日期"] as string ?? "",
                序列号 = reader["序列号"] as string ?? "",
                CPU = reader["CPU"] as string ?? "",
                主板 = reader["主板"] as string ?? "",
                内存 = reader["内存"] as string ?? "",
                硬盘 = reader["硬盘"] as string ?? "",
                显卡 = reader["显卡"] as string ?? "",
                显示器 = reader["显示器"] as string ?? "",
                打印机 = reader["打印机"] as string ?? "",
                网卡 = reader["网卡"] as string ?? "",
                IP地址 = reader["IP地址"] as string ?? "",
                MAC地址 = reader["MAC地址"] as string ?? "",
                应用程序 = reader["应用程序"] as string ?? "",
                姓名 = reader["姓名"] as string ?? "",
                位置 = reader["位置"] as string ?? "",
                备注 = reader["备注"] as string ?? "",
                提交时间 = DateTime.Parse(reader["提交时间"].ToString())
            };
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}
