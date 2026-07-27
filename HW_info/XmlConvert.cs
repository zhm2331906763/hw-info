using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HW_info
{
    public class XmlConvert
    {
		public static T Deserialize<T>(string xml) where T : class
		{
			try
			{
				var bytes = Encoding.UTF8.GetBytes(xml);
				using (var sr = new MemoryStream(bytes))
				{
					XmlSerializer xz = new XmlSerializer(typeof(T));
					return (T)xz.Deserialize(sr);
				}
			}
			catch (Exception)
			{
				return default;
			}
		}

		public static string Serializer<T>(T obj) where T : class
        {
            try
            {
				using (var ms = new MemoryStream())
				{
					using (var sw = new StreamWriter(ms, Encoding.UTF8))
					{
						XmlSerializer xz = new XmlSerializer(typeof(T));
						XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
						ns.Add("Name", AppDomain.CurrentDomain.FriendlyName);
						xz.Serialize(sw, obj, ns);
						sw.Flush();
						return Encoding.UTF8.GetString(ms.ToArray());
					}
				}
			}
            catch (Exception)
            {
				return default;
            }
			
		}
	}
}
