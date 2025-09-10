using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RDPTimeGrabber
{
    public class UserUpdater
    {
        public static bool UpdateUsers(Config config)
        {
            if (DateTime.Now - config.LastUserUpdateTime < TimeSpan.FromDays(1))
                return false;

            var req = "!users\r\n";
            GetUsers(ref req);
            req += "!logs\r\n!end";
            File.WriteAllText("last_users_upd.csv", req);
            {
                Console.WriteLine("Отправка на сервер пользователей");
                var result = HttpUtils.Request(config.ServerUrl + "dataupload", "POST", req);

                if (result == HttpStatusCode.OK)
                {
                    HttpUtils.Request(config.ServerUrl + "syncscud/salavat", "POST");
                    HttpUtils.Request(config.ServerUrl + "syncscud/ufa", "POST");

                    Console.WriteLine("Успешно!");

                    config.LastUserUpdateTime = DateTime.Now;
                    return true;
                }
            }
            return false;
        }


        private static void GetUsers(ref string req_)
        {
            using (var context = new PrincipalContext(ContextType.Domain, "SNHPRO"))
            {
                using (var searcher = new PrincipalSearcher(new UserPrincipal(context) { Enabled = true }))
                {
                    foreach (var result in searcher.FindAll())
                    {
                        var de = result.GetUnderlyingObject() as DirectoryEntry;
                        var up = result as UserPrincipal;
                        if (de.Properties["objectCategory"].Value != null && de.Properties["givenName"].Value != null && de.Properties["sn"].Value != null && de.Properties["objectCategory"].Value.ToString().Contains("Person") && !string.IsNullOrWhiteSpace(de.Properties["givenName"].Value.ToString()) && !string.IsNullOrWhiteSpace(de.Properties["sn"].Value.ToString()))
                        {
                            req_ += $"SNHPRO\\{de.Properties["samAccountName"].Value};{de.Properties["sn"].Value} {de.Properties["givenName"].Value}\r\n";
                        }
                    }
                }
            }
            using (var context = new PrincipalContext(ContextType.Domain, "UFA"))
            {
                using (var searcher = new PrincipalSearcher(new UserPrincipal(context) { Enabled = true }))
                {
                    foreach (var result in searcher.FindAll())
                    {
                        var de = result.GetUnderlyingObject() as DirectoryEntry;
                        var up = result as UserPrincipal;
                        if (de.Properties["objectCategory"].Value != null && de.Properties["givenName"].Value != null && de.Properties["sn"].Value != null && de.Properties["objectCategory"].Value.ToString().Contains("Person") && !string.IsNullOrWhiteSpace(de.Properties["givenName"].Value.ToString()) && !string.IsNullOrWhiteSpace(de.Properties["sn"].Value.ToString()))
                        {
                            req_ += $"UFA\\{de.Properties["samAccountName"].Value};{de.Properties["sn"].Value} {de.Properties["givenName"].Value}\r\n";
                        }
                    }
                }
            }
        }
    }
}
