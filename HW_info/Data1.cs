using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HW_info
{
    public static class Data1
    {
        //文件名
        static readonly string fileName = "HW_info_Datas.xml";

        //数据列表
        static List<MyData> MyDatas { get; set; } = new List<MyData>();

        /// <summary>
        /// 全部数据
        /// </summary>
        public static IEnumerable<MyData> Datas => MyDatas;

        /// <summary>
        /// 总数
        /// </summary>
        public static int Count => MyDatas.Count;

        /// <summary>
        /// 去重后的数据
        /// </summary>
        public static IEnumerable<MyData> Distinct => MyDatas.Distinct(new MyData()).ToList();

        /// <summary>
        /// 新提交的数据
        /// </summary>
        public static IEnumerable<MyData> Latest
        {
            get
            {
                var list = new List<MyData>();
                for (int i = MyDatas.Count - 1; i >= 0; i--)
                {
                    var y = MyDatas[i];
                    var flag = list.Any(x => x.MAC地址 == y.MAC地址);
                    if (!flag) list.Add(y);
                }
                return list;
            }
        }

        //锁
        static readonly object obj = new object();
        //添加
        public static void Add(MyData data)
        {
            if (data != null)
            {
                data.提交时间 = DateTime.Now;  //以服务端时间为准
                lock (obj)
                {
                    MyDatas.Add(data);
                    Save();
                }
            }
        }

        //构造函数自动载入
        static Data1()
        {
            Load();
        }

        static void Load()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if (!File.Exists(path)) return;
            var xmlText = File.ReadAllText(path);
            MyDatas = XmlConvert.Deserialize<List<MyData>>(xmlText);
        }

        static void Save()
        {
            var xmlText = XmlConvert.Serializer(MyDatas);
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            File.WriteAllText(path, xmlText);
        }


        public static string ToCsv(IEnumerable<MyData> datas)
        {
            return ConvertListToCsv(datas);
        }

        static string ConvertListToCsv<T>(IEnumerable<T> list, bool includeHeader = true)
        {
            if (list == null) return string.Empty;

            var sb = new StringBuilder();
            var type = typeof(T);
            var properties = type.GetProperties();

            // 添加表头
            if (includeHeader)
            {
                var header = properties.Select(p => $"\"{p.Name}\"");
                sb.AppendLine(string.Join(",", header));
            }

            // 添加数据行
            foreach (var item in list)
            {
                var fields = properties.Select(p => $"\"{p.GetValue(item)}\"");
                sb.AppendLine(string.Join(",", fields));
            }

            return sb.ToString();
        }


        public static string ToHtml(IEnumerable<MyData> datas)
        {
            var dt = ToDataTable(datas);
            return DTtoHtml(dt);
        }

        static DataTable ToDataTable<T>(IEnumerable<T> list)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);

            // 获取对象的所有属性
            var properties = typeof(T).GetProperties();

            // 创建DataTable的列（与对象属性对应）
            foreach (var prop in properties)
            {
                dataTable.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            // 填充数据行
            foreach (T item in list)
            {
                DataRow row = dataTable.NewRow();
                foreach (var prop in properties)
                {
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                }
                dataTable.Rows.Add(row);
            }
            return dataTable;
        }

        static string DTtoHtml(DataTable dt)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<html><head>");
            sb.Append($"<title>HW_info 服务端 2.0</title>");
            sb.Append("<meta http-equiv='content-type' content='text/html; charset=GB2312'> ");
            sb.Append("<style type=text/css>");
            sb.Append("li{font-size: 9pt; display: inline-block; margin-right: 10px;}");
            sb.Append("td{font-size: 9pt;border:solid 1 #000000;word-break: break-all;}");
            sb.Append("table{padding:3 0 3 0;border:solid 1 #000000;margin:0 0 0 0;BORDER-COLLAPSE: collapse; table-layout:fixed;}");
            sb.Append("select{width: 100%;}");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body>");
            sb.Append("<li><a href='/'>主页</a></li>");
            sb.Append("<li><a href='/Datas'>全部</a></li>");
            sb.Append("<li><a href='/Distinct'>去重</a></li>");
            sb.Append("<li><a href='/Latest'>最新</a></li>");
            sb.Append("<p></p>");
            sb.Append("<table cellSpacing='0' cellPadding='0' width ='100%' border='1'>");
            sb.Append("<tr valign='middle'>");
            sb.Append("<td><b>序号</b></td>");
            foreach (DataColumn column in dt.Columns)
            {
                sb.Append("<td><b><span>" + column.ColumnName + "</span></b></td>");
            }
            sb.Append("</tr>");
            int iColsCount = dt.Columns.Count;
            int rowsCount = dt.Rows.Count - 1;
            for (int j = 0; j <= rowsCount; j++)
            {
                sb.Append("<tr>");
                sb.Append("<td>" + ((int)(j + 1)).ToString() + "</td>");
                for (int k = 0; k <= iColsCount - 1; k++)
                {
                    sb.Append("<td>");
                    string strCellContent = Convert.ToString(dt.Rows[j][k]) ?? "&nbsp;";
                    if (dt.Columns[k].ColumnName == "应用程序")
                    {
                        sb.Append("<select>");
                        var list = strCellContent.Split('|');
                        var option = string.Join("", list.Select(x => $"<option>{x}</option>"));
                        sb.Append(option);
                        sb.Append("</select>");
                    }
                    else
                    {
                        sb.Append("<span>" + strCellContent + "</span>");
                    }
                    sb.Append("</td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            sb.Append("</body></html>");
            return sb.ToString();
        }



    }
}
