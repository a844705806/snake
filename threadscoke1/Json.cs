using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//有关JSON的包
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace new_scoket
{
    class Json
    {
        public static object funcJsonStr2Obj(string json_str)
        {
            lock (json_str)
            {
                return new JavaScriptSerializer().DeserializeObject(json_str);
            }
        }

        public static IDictionary<string, object> funcJsonStr2Obj_dict(string json_str)
        {
            lock (json_str)
            {
                return (IDictionary<string, object>)funcJsonStr2Obj(json_str);
            }
        }


        public static string funcObj2JsonStr(object obj)
        {
            lock (obj)
            {
                return JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.None);
            }
        }
    }
}
