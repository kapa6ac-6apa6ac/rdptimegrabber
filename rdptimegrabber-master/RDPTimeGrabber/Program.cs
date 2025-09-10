using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RDPTimeGrabber
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = Config.Load();

            var timeGrabbed = TimeGrabber.SendTime(config);
            var userGrabbed = UserUpdater.UpdateUsers(config);

            if (timeGrabbed || userGrabbed)
                config.Save();
        }
    }

    [Serializable]
    public class ConnectionLog
    {
        public DateTime Date { get; set; }
        public string Username { get; set; }
        public string IpAddress { get; set; }
        public string Resource { get; set; }
        public ulong SessionDuration { get; set; }
    }

    public class Config
    {
        public string ServerUrl { get; set; } = "http://172.17.0.185/api/";
        public DateTime LastRunTime { get; set; } = DateTime.Now;
        public DateTime LastUserUpdateTime { get; set; } = DateTime.Now;

        public static Config Load()
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
        }

        public void Save()
        {
            File.WriteAllText("config.json", Newtonsoft.Json.JsonConvert.SerializeObject(this));
        }
    }
}
