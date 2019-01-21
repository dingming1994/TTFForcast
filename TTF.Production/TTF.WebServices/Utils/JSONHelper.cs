using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Data;
using Newtonsoft.Json;
namespace TTF.Utils
{
    public static class JSONHelper
    {
        public static string ToJSON(this object obj)
        {
            JavaScriptSerializer s = new JavaScriptSerializer();
            s.MaxJsonLength = Int32.MaxValue;
            return s.Serialize(obj);
        }
        public static string ToJSON(this object obj, int recursionDepth)
        {
            JavaScriptSerializer s = new JavaScriptSerializer();
            s.MaxJsonLength = Int32.MaxValue;
            s.RecursionLimit = recursionDepth;
            return s.Serialize(obj);
        }
        public static string GetJson(DataTable dt)
        {
            System.Web.Script.Serialization.JavaScriptSerializer serializer = new

            System.Web.Script.Serialization.JavaScriptSerializer();
            List<Dictionary<string, object>> rows =
              new List<Dictionary<string, object>>();
            Dictionary<string, object> row = null;

            foreach (DataRow dr in dt.Rows)
            {
                row = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    row.Add(col.ColumnName.Trim(), dr[col]);
                }
                rows.Add(row);
            }
            return serializer.Serialize(rows);
        }
        public static T DeserializeObject<T>(string value)
        {
            // JavaScriptSerializer js = new JavaScriptSerializer();
            //js.MaxJsonLength = int.MaxValue;
            //ClientDataManagerData data = JSONHelper.DeserializeObject<ClientDataManagerData>(resultJSON);
            //Using newtonsoft to optimize performance
            return JsonConvert.DeserializeObject<T>(value);
        }
    }
}
