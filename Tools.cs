﻿using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Diagnostics;
using Microsoft.Win32;

namespace SimpleWebServer
{
    internal static class Tools
    {
        public static void Log(string msg, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
        }

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
                Log("LAN IP Address not found.");
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
                Log("Usage: SimpleWebServer.exe [folderpath] [port]");
                Log("Do you want to install Context menu item? (y/n)");
                var res = Console.ReadLine();
                if (res == "y")
                {
                    InstallContextMenu();
                    Log("You can now close this window and launch application from Explorer folder Context menu.");
                    Console.ReadLine();
                }
                return null;
            }

            // if only 1 argument, check if its path or port number
            if (args.Length == 1)
            {
                // check if its Install or Uninstall
                if (args[0].ToLower() == "install")
                {
                    Log("Installing context menu...");
                    InstallContextMenu();
                    return null;
                }

                if (args[0].ToLower() == "uninstall")
                {
                    Log("Uninstalling context menu...");
                    UninstallContextMenu();
                    return null;
                }

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
                    Log("Usage: SimpleWebServer.exe [folderpath] [port]");
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
                Log("Path " + args[0] + " does not exist.", ConsoleColor.Red);
                Console.ReadLine();
                return null;
            }

            // check if tcp listener port is available
            if (Tools.PortAvailable(args[1]) == false)
            {
                Log("Port " + args[1] + " is not available.", ConsoleColor.Red);
                Console.ReadLine();
                return null;
            }
            return args;
        }

        internal static void InstallContextMenu()
        {
            string contextRegRoot = "Software\\Classes\\Directory\\Background\\shell";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(contextRegRoot, true);

            // add folder if missing
            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\Background\Shell");
            }

            if (key != null)
            {
                var appName = "SimpleWebBrowser";
                key.CreateSubKey(appName);

                key = key.OpenSubKey(appName, true);
                key.SetValue("", "Start " + appName + " here");
                key.SetValue("Icon", "\"" + Process.GetCurrentProcess().MainModule.FileName + "\"");

                key.CreateSubKey("command");
                key = key.OpenSubKey("command", true);
                var executeString = "\"" + Process.GetCurrentProcess().MainModule.FileName + "\"";
                // TODO add port
                executeString += " \"%V\"";
                key.SetValue("", executeString);
                Log("Installed context menu item!");
            }
            else
            {
                Log("Error> Cannot find registry key: " + contextRegRoot, ConsoleColor.Red);
            }
        }

        public static void UninstallContextMenu()
        {
            string contextRegRoot = "Software\\Classes\\Directory\\Background\\shell";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(contextRegRoot, true);
            if (key != null)
            {
                var appName = "SimpleWebBrowser";
                RegistryKey appKey = Registry.CurrentUser.OpenSubKey(contextRegRoot + "\\" + appName, false);
                if (appKey != null)
                {
                    key.DeleteSubKeyTree(appName);
                    Log("Removed context menu item!");
                }
                else
                {
                    //Console.WriteLine("Nothing to uninstall..");
                }
            }
            else
            {
                Log("Error> Cannot find registry key: " + contextRegRoot, ConsoleColor.Red);
            }
        }

        internal static void LaunchBrowser(string url)
        {
            Log("Launching browser: " + url);
            try
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log("Error launching browser: " + ex.Message, ConsoleColor.Red);
            }
        }

        internal static void RestartAsAdmin(string[] args)
        {
            try
            {
                // Get the path to the current executable
                string exePath = Process.GetCurrentProcess().MainModule.FileName;

                // Create a new process with elevated permissions
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = exePath;
                startInfo.Arguments = string.Join(" ", args); // Pass the same arguments
                startInfo.Verb = "runas"; // Run as administrator

                Process.Start(startInfo);

                // Exit the current process
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Log("Error restarting as administrator: " + ex.Message, ConsoleColor.Red);
            }
        }

        internal static void PrintBanner()
        {
            string banner = @" _____ _           _     _ _ _     _   _____                     
|   __|_|_____ ___| |___| | | |___| |_|   __|___ ___ _ _ ___ ___ 
|__   | |     | . | | -_| | | | -_| . |__   | -_|  _| | | -_|  _|
|_____|_|_|_|_|  _|_|___|_____|___|___|_____|___|_|  \_/|___|_|  
              |_|";

            Tools.Log(banner, ConsoleColor.Cyan);
            Tools.Log("https://github.com/unitycoder/SimpleWebServer\n", ConsoleColor.DarkGray);

        }


    } // class
} // namespace
