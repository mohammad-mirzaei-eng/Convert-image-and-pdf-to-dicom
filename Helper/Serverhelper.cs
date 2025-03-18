using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Convert_to_dcom.Class.Helper
{
    public static class Serverhelper
    {

        public static bool IsValidIP(string serverAddress)
        {
            return IPAddress.TryParse(serverAddress, out _);
        }


        public static bool IsServerReachable(SettingsModel settingsModel)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(settingsModel.ServerAddress, settingsModel.ServerPort, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                    if (!success)
                    {
                        return false;
                    }
                    client.EndConnect(result);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
