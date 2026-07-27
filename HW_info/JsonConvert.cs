using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace HW_info
{

    public class JsonConvert
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public static T Deserialize<T>(string json) 
        {
            try
            {
                return Serializer.Deserialize<T>(json);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static string Serialize<T>(T obj) 
        {
            try
            {
                return Serializer.Serialize(obj);
            }
            catch (Exception)
            {
                return default;
            }
        }

    }
}
