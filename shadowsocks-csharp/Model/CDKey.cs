using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shadowsocks.Model
{
    public class CDKey
    {
        public static Random r = new Random();
        public static string generateCDKEY(string method, string password, string server, int port)
        {
            var base64 = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(method + "," + password + "," + server + "," + port));
            var salt = r.NextDouble().ToString("0.000000000").Substring(2);
            var prefix = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(salt));
            return base64.TrimEnd('=') + prefix;
        }

        public static Server readCDKEY(string cdkey)
        {
            try
            {
                cdkey = cdkey.Substring(0, cdkey.Length - 12);
                while (cdkey.Length % 4 != 0)
                {
                    cdkey = cdkey + "=";
                }

                cdkey = UTF8Encoding.UTF8.GetString(Convert.FromBase64String(cdkey));
                var temp = cdkey.Split(',');
                return new Server()
                {
                    method = temp[0],
                    password = temp[1],
                    server = temp[2],
                    server_port = int.Parse(temp[3])
                };

            }
            catch (Exception)
            {
                return new Server();
            }
        }
    }
}
