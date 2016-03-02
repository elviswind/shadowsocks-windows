using System;
using System.Collections.Generic;
using System.IO;

using Shadowsocks.Controller;
using Newtonsoft.Json;

namespace Shadowsocks.Model
{
    [Serializable]
    public class Configuration
    {
        public List<Server> configs;

        // when strategy is set, index is ignored
        public string strategy;
        public string cdkey;
        public int index;
        public bool global;
        public bool enabled;
        public bool shareOverLan;
        public bool isDefault;
        public int localPort;
        public string pacUrl;
        public bool useOnlinePac;
        public bool availabilityStatistics;
        public bool autoCheckUpdate;
        public LogViewerConfig logViewer;

        private static string CONFIG_FILE = "config.json";

        public Server GetCurrentServer()
        {
            return GetServer(cdkey);
        }

        public static void CheckServer(Server server)
        {
            CheckPort(server.server_port);
            CheckPassword(server.password);
            CheckServer(server.server);
        }

        public static Configuration Load()
        {
            try
            {
                string configContent = File.ReadAllText(CONFIG_FILE);
                Configuration config = JsonConvert.DeserializeObject<Configuration>(configContent);
                config.configs = new List<Server>()
                {
                    GetServer(config.cdkey)
                };
                config.isDefault = false;
                if (config.localPort == 0)
                    config.localPort = 1080;
                if (config.index == -1 && config.strategy == null)
                    config.index = 0;
                return config;
            }
            catch (Exception e)
            {
                if (!(e is FileNotFoundException))
                    Logging.LogUsefulException(e);
                return new Configuration
                {
                    index = 0,
                    enabled = true,
                    isDefault = true,
                    localPort = 1080,
                    autoCheckUpdate = true,
                    configs = new List<Server>()
                    {
                        GetServer(null)
                    }
                };
            }
        }

        public static void Save(Configuration config)
        {
            var configs = config.configs;

            config.index = 0;
            config.isDefault = false;
            config.configs = null;
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(CONFIG_FILE, FileMode.Create)))
                {
                    string jsonString = JsonConvert.SerializeObject(config, Formatting.Indented);
                    sw.Write(jsonString);
                    sw.Flush();
                }
            }
            catch (IOException e)
            {
                Console.Error.WriteLine(e);
            }

            config.configs = configs;
        }

        public static Server GetServer(string cdkey)
        {
            if (cdkey == null)
            {
                return new Server();
            }
            else
            {
                return CDKey.readCDKEY(cdkey);
            }
        }

        private static void Assert(bool condition)
        {
            if (!condition)
                throw new Exception(I18N.GetString("assertion failure"));
        }

        public static void CheckPort(int port)
        {
            if (port <= 0 || port > 65535)
                throw new ArgumentException(I18N.GetString("Port out of range"));
        }

        public static void CheckLocalPort(int port)
        {
            CheckPort(port);
            if (port == 8123)
                throw new ArgumentException(I18N.GetString("Port can't be 8123"));
        }

        private static void CheckPassword(string password)
        {
            if (password.IsNullOrEmpty())
                throw new ArgumentException(I18N.GetString("Password can not be blank"));
        }

        private static void CheckServer(string server)
        {
            if (server.IsNullOrEmpty())
                throw new ArgumentException(I18N.GetString("Server IP can not be blank"));
        }
    }
}
