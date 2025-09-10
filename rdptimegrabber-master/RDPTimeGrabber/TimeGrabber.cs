using System;
using System.Collections.Generic;
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
    public class TimeGrabber
    {
        public static bool SendTime(Config config)
        {
            Console.WriteLine("Чтение конфига");
            var startTime = DateTime.UtcNow;

            string logType = "Microsoft-Windows-TerminalServices-Gateway/Operational";
            string query = $"*[System[(EventID=303) and TimeCreated[@SystemTime>='{config.LastRunTime.AddSeconds(-5).ToString("o")}']]]";

            Console.WriteLine("Чтение логов");

            var session = new EventLogSession();
            var elQuery = new EventLogQuery(logType, PathType.LogName, query);
            elQuery.Session = session;
            var elReader = new EventLogReader(elQuery);

            //var elReader = new EventLogReader("rdp.evtx", PathType.FilePath);

            List<ConnectionLog> logs = new List<ConnectionLog>();

            for (EventRecord eventInstance = elReader.ReadEvent(); eventInstance != null; eventInstance = elReader.ReadEvent())
            {
                logs.Add(new ConnectionLog
                {
                    Date = eventInstance.TimeCreated.Value,
                    Username = eventInstance.Properties[0].Value as string,
                    IpAddress = eventInstance.Properties[1].Value as string,
                    Resource = eventInstance.Properties[3].Value as string,
                    SessionDuration = ulong.Parse(eventInstance.Properties[6].Value as string)
                });
            }

            Console.WriteLine("Получение информации о пользователях");
            var req = "!users\r\n";
            var users = logs.Select(l => l.Username).Distinct().OrderBy(l => l).ToArray();

            if (users.Length == 0)
                return false;

            foreach (var user in users)
            {
                var u = user.Split('\\');
                using (var domainContext = new PrincipalContext(ContextType.Domain, u[0]))
                using (var puser = new UserPrincipal(domainContext) { SamAccountName = u[1] })
                using (var ps = new PrincipalSearcher() { QueryFilter = puser })
                {
                    var res = ps.FindAll().ToList();
                    if (res.Count == 0)
                        req += $"{user};\r\n";
                    else
                        req += $"{user};{res[0].Name}\r\n";
                }

            }
            req += "!logs\r\n";

            var dates = logs.Select(d => d.Date.Date).Distinct();

            Console.WriteLine("Запись данных");
            foreach (var log in logs)
            {
                if (log.SessionDuration > 0)
                    req += $"{log.Username};{log.Date.Ticks};{log.Resource};{log.SessionDuration};{log.IpAddress}\r\n";
            }
            req += "!end";

            File.WriteAllText("req.csv", req);
            {
                Console.WriteLine("Отправка на сервер времени");
                var result = HttpUtils.Request(config.ServerUrl + "dataupload", "POST", req);
                if (result == HttpStatusCode.OK)
                {
                    config.LastRunTime = startTime;
                    return true;
                }
            }

            return false;
        }
    }
}
