using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qdf_test_wpf_1
{
    class Command
    {

        public String name;
        public Dictionary<string, string> attrs = new Dictionary<String, String>();
	
	    public static Command fromString(String str)
        {
            try
            {
                str = str.Replace("\r", "").Replace("\n", "");

                Command c = new Command();

                int sepLocation = str.IndexOf(':');
                if (sepLocation != -1)
                {
                    c.name = str.Substring(0, sepLocation);
                    String paramsStr = str.Substring(sepLocation + 1);
                    String[] paramPairs = paramsStr.Split(',');
                    foreach (String paramPair in paramPairs)
                    {
                        String[] kv = paramPair.Split('=');
                        String k = kv[0];
                        String v = kv[1];
                        c.attrs.Add(k, v);
                    }
                }
                else
                {
                    c.name = str;
                }

                return c;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        
        String toString()
        {
            StringBuilder sb = new StringBuilder(this.name);
            if (attrs.Keys.Count > 0){
                sb.Append(":");
                foreach (String k in attrs.Keys)
                {
                    sb.Append(k).Append("=").Append(attrs[k]).Append(",");
                }
                sb.Remove(sb.Length - 1, 1); //去掉最后一个逗号
            }

            return sb.ToString();
        }

    }
}
