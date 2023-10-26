using System.Net.Sockets;
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

                // check if already exists in registry
                if (IsInstalledInRegistry())
                {
                    Log("Good. Explorer Context menu is already installed, you can use it there.");
                }
                else
                {
                    // TODO make enter as "y" default
                    Log("Do you want to install Context menu item? (y/N)");
                    var res2 = Console.ReadLine();
                    if (res2 == "y")
                    {
                        InstallContextMenu();
                    }
                }

                string exePath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);

                if (IsAlreadyAddedToPath())
                {
                    Log("Good. Application folder is already added to user environment PATH, you can use it from anywhere in command line.");
                }
                else
                {
                    // TODO make enter as "y" default
                    Log("Do you want to add exe path (" + exePath + ") to user environment PATH? (y/N)");
                    var res = Console.ReadLine();
                    if (res == "y")
                    {
                        ModifyUserEnvPATH(add: true);
                    }
                }

                // load settings
                var settings = LoadSettings();
                string prevProjectPath;
                string prevPort;
                settings.TryGetValue("ProjectPath", out prevProjectPath);

                if (string.IsNullOrEmpty(prevProjectPath) == false && Directory.Exists(prevProjectPath) == true)
                {
                    // TODO make enter as "y" default
                    Log("Do you want to start in previous Project folder: " + prevProjectPath + " ? (y/N)");
                    var res = Console.ReadLine();
                    if (res == "y")
                    {
                        args = new string[2];
                        args[0] = prevProjectPath;

                        settings.TryGetValue("Port", out prevPort);
                        if (int.TryParse(prevPort, out int portNumber) == false) prevPort = defaultPort;
                        args[1] = string.IsNullOrEmpty(prevPort) ? defaultPort : prevPort;
                        return args;
                    }
                }

                // ask if want to start in current folder
                Log("Do you want to start server in the current folder: " + exePath + " ? (y/N)");
                var res3 = Console.ReadLine();
                if (res3 == "y")
                {
                    args = new string[2];
                    args[0] = exePath;
                    args[1] = defaultPort;
                    return args;
                }

                Log("You can now close this window");
                Console.ReadLine();

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

        static Dictionary<string, string> LoadSettings()
        {
            var settings = new Dictionary<string, string>();
            // load from roaming folder
            var roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var settingsFile = Path.Combine(roamingFolder, "SimpleWebServer", "config.ini");

            if (File.Exists(settingsFile))
            {
                Log("Loading settings from: " + settingsFile, ConsoleColor.DarkGray);

                var lines = File.ReadAllLines(settingsFile);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        settings[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }
            return settings;
        }

        internal static void SaveSettings(string[] settings)
        {
            // load from roaming folder
            var roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var settingsFile = Path.Combine(roamingFolder, "SimpleWebServer", "config.ini");

            try
            {
                // if folder is missing, create it
                var folder = Path.GetDirectoryName(settingsFile);
                if (Directory.Exists(folder) == false)
                {
                    Directory.CreateDirectory(folder);
                }

                var lines = new List<string>();
                foreach (var setting in settings)
                {
                    // if number, its port, if path, its project path
                    if (int.TryParse(setting, out int portNumber))
                    {
                        if (portNumber >= 80 && portNumber < 65535) lines.Add("Port=" + portNumber);
                    }
                    else
                    {
                        if (Directory.Exists(setting)) lines.Add("ProjectPath=" + setting);
                    }
                }
                File.WriteAllLines(settingsFile, lines);
                Log("Saved settings to " + settingsFile, ConsoleColor.DarkGray);
            }
            catch (Exception)
            {
                Log("Failed to save settings to " + settingsFile, ConsoleColor.Red);
                throw;
            }
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

        static bool IsInstalledInRegistry()
        {
            string contextRegRoot = "Software\\Classes\\Directory\\Background\\shell";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(contextRegRoot, true);
            if (key != null)
            {
                var appName = "SimpleWebBrowser";
                RegistryKey appKey = Registry.CurrentUser.OpenSubKey(contextRegRoot + "\\" + appName, false);
                if (appKey != null)
                {
                    return true;
                }
            }
            return false;
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

            Log(banner, ConsoleColor.Cyan);
            Log("https://github.com/unitycoder/SimpleWebServer\n", ConsoleColor.DarkGray);

        }

        static bool IsAlreadyAddedToPath()
        {
            string executablePath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            string path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
            return path.Contains(executablePath);
        }

        // WARNING: if user has the exe in common already existing path, it will be removed!
        internal static void ModifyUserEnvPATH(bool add)
        {
            string executablePath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            string path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

            if (path.Contains(executablePath) == false)
            {
                if (add == true)
                {
                    path = $"{path};{executablePath}";
                    Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.User);
                    Log("Directory added to user PATH successfully.", ConsoleColor.Gray);
                    Log("NOTE: You need to restart PC or Logout/Login, for changes to take effect in certain applications, like Unity Editor.", ConsoleColor.Yellow);
                }
                else // remove
                {
                    Log("Directory is not in PATH.", ConsoleColor.Yellow);
                }
            }
            else // already added
            {
                if (add == true)
                {
                    Log("Directory is already in PATH.", ConsoleColor.Yellow);
                }
                else
                {
                    path = path.Replace(executablePath, "");
                    Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.User);
                    Log("Directory removed from user PATH successfully.", ConsoleColor.Gray);
                }
            }
        }

    } // class
} // namespace
