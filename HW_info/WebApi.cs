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
        private readonly Dictionary<string, string> _staticFiles;

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

        public bool TryGetBinaryExport(string path, out byte[] data, out string contentType)
        {
            if (path == "/api/export/xlsx")
            {
                data = DataService.ExportXlsx();
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return true;
            }
            data = null;
            contentType = null;
            return false;
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
