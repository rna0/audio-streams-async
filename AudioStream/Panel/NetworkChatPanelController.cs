using System;
using System.Globalization;
using System.Net;

namespace AudioStream.Panel
{
    public static class NetworkChatPanelController
    {
        public static int GetIntFromUser()
        {
            if (int.TryParse(Console.ReadLine(), out var output)) return output;
            Console.WriteLine(@"Expected integer");
            return 0;
        }

        public static void ShowIpAndPort(string ipAddr, string textPort)
        {
            Console.WriteLine($@"# {ipAddr}:{textPort} is your current Connection parameters.");
        }

        /// <summary>
        /// try creating IPEndPoint object from IP:port string
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns>IPEndPoint connection details for streaming</returns>
        public static IPEndPoint CreateIpEndPoint(string endPoint)
        {
            var ipSplitArray = endPoint.Split(':');
            if (ipSplitArray.Length < 2) throw new FormatException("Invalid endpoint format");
            IPAddress ip;
            if (ipSplitArray.Length > 2)
            {
                if (!IPAddress.TryParse(string.Join(":", ipSplitArray, 0, ipSplitArray.Length - 1), out ip))
                {
                    throw new FormatException("Invalid ip-address");
                }
            }
            else
            {
                if (!IPAddress.TryParse(ipSplitArray[0], out ip))
                {
                    throw new FormatException("Invalid ip-address");
                }
            }

            if (!int.TryParse(ipSplitArray[^1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out var port))
            {
                throw new FormatException("Invalid port");
            }

            return new IPEndPoint(ip, port);
        }
    }
}