using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Principal;

namespace SimpleWebServer
{
    internal static class Tools
    {
        // get LAN IP address starting with 192.
        public static object GetIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString().StartsWith("192."));

            if (ipAddress != null)
            {
                //Console.WriteLine("LAN IP Address: " + ipAddress.ToString());
                return ipAddress.ToString();
            }
            else
            {
                Console.WriteLine("LAN IP Address not found.");
                return null;
            }
        }

        public static bool PortAvailable(string port)
        {
            bool portAvailable = true;
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endpoint in tcpConnInfoArray)
            {
                if (endpoint.Port.ToString() == port)
                {
                    portAvailable = false;
                    break;
                }
            }

            return portAvailable;
        }

        public static bool IsUserAnAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            // Check for the admin SIDs
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static string[]? ValidateArguments(string[] args, string defaultPort)
        {
            // if too many arguments, show usage
            if (args == null || args.Length > 2 || args.Length < 1)
            {
                Console.WriteLine("Usage: SimpleWebServer.exe [folderpath] [port]");
                Console.ReadLine();
                return null;
            }

            // if only 1 argument, check if its path or port number
            if (args.Length == 1)
            {
                // if its a path, use it
                if (Directory.Exists(args[0]))
                {
                    var temp = args[0];
                    args = new string[2];
                    args[0] = temp;
                    args[1] = defaultPort;
                }

                // if its a port number, use current exe folder and that port
                else if (int.TryParse(args[0], out int portNumber))
                {
                    args = new string[2];
                    args[0] = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    args[1] = portNumber.ToString();
                }
                // if its not a path or port number, show usage
                else
                {
                    Console.WriteLine("Usage: SimpleWebServer.exe [folderpath] [port]");
                    Console.ReadLine();
                    return null;
                }
            }

            // if no 2 arguments, use current exe folder and port 8080
            if (args.Length < 2)
            {
                args = new string[2];
                args[0] = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                args[1] = defaultPort;
            }

            // check if path exists
            if (!Directory.Exists(args[0]))
            {
                Console.WriteLine("Path " + args[0] + " does not exist.");
                Console.ReadLine();
                return null;
            }

            // check if tcp listener port is available
            if (Tools.PortAvailable(args[1]) == false)
            {
                Console.WriteLine("Port " + args[1] + " is not available.");
                Console.ReadLine();
                return null;
            }
            return args;
        }

    } // class
}
