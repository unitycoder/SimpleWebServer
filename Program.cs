using System.Net;
using System.Text;

namespace SimpleWebServer
{
    internal class Program
    {
        static readonly string appID = "SimpleWebServer";
        static string rootFolder = "";
        static bool allowExternalConnections = false;
        static string defaultPort = "8080";

        static void Main(string[] args)
        {
            Tools.PrintBanner();

            args = Tools.ValidateArguments(args, defaultPort);
            if (args == null) return;

            rootFolder = args[0];
            string port = args[1];
            Tools.Log("Serving directory: " + args[0]);

            Tools.SaveSettings(args);
            StartServer(port);

            // launch browser
            string url = $"http://localhost:{port}/";
            Tools.LaunchBrowser(url);

            Tools.Log("Press F1/F2 to Install/Uninstall Explorer Context menu");
            Tools.Log("Press F3/F4 to add/remove executable on User environment PATH");
            Tools.Log("Press F5 to open Browser");
            // wait for keypress to restart as admin
            if (Tools.IsUserAnAdmin() == false)
            {
                Tools.Log("Press Enter to exit, or F12 to run as Admin (to allow external connections)");
                Tools.Log("------------------------------------------------");
                while (true)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.F1) Tools.InstallContextMenu();
                    if (k.Key == ConsoleKey.F2) Tools.UninstallContextMenu();
                    if (k.Key == ConsoleKey.F3) Tools.ModifyUserEnvPATH(add: true);
                    if (k.Key == ConsoleKey.F4) Tools.ModifyUserEnvPATH(add: false);
                    if (k.Key == ConsoleKey.F12) Tools.RestartAsAdmin(args);
                }
            }
            else
            {
                Tools.Log("Press Enter to exit.");
                Tools.Log("------------------------------------------------");
            }

            Console.ReadLine();
        }

        private static void StartServer(string port)
        {
            // start server
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");

            // check if runas admin
            bool isAdmin = Tools.IsUserAnAdmin();

            if (isAdmin == true)
            {
                // then allow external connections
                allowExternalConnections = true;
                Tools.Log("The application is running as an administrator. External connections are allowed!");
                // NOTE using hostname ipaddress requires admin rights
                var ipAddress = Tools.GetIpAddress();
                if (string.IsNullOrEmpty(ipAddress.ToString()) == false)
                {
                    listener.Prefixes.Add($"http://{ipAddress}:{port}/");
                }
            }
            else
            {
                Tools.Log("The application is not running as an administrator.");
            }

            foreach (string prefix in listener.Prefixes)
            {
                Tools.Log("Listening: " + prefix, ConsoleColor.Green);
            }

            listener.Start();
            listener.BeginGetContext(RequestHandler, listener);
        }

        static void RequestHandler(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = listener.EndGetContext(result);

            // Start accepting the next request asynchronously
            listener.BeginGetContext(RequestHandler, listener);

            try
            {
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                // set headers to disable caching
                response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                response.Headers["Pragma"] = "no-cache";
                response.Headers["Expires"] = "0";

                string path = Uri.UnescapeDataString(context.Request.Url.LocalPath);

                if (path == "/")
                {
                    path = "/index.html";
                }

                if (Path.GetExtension(path) == ".html" || path.EndsWith(".js") || path.EndsWith(".js.gz") || path.EndsWith(".js.br"))
                {
                    response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
                    response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
                    response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
                }

                // unity webgl support
                if (Path.GetExtension(path) == ".gz")
                {
                    response.AddHeader("Content-Encoding", "gzip");
                }
                else if (Path.GetExtension(path) == ".br")
                {
                    response.AddHeader("Content-Encoding", "br");
                }

                if (context.Request.Headers.Get("Range") != null)
                {
                    response.AddHeader("Accept-Ranges", "bytes");
                }

                if (path.EndsWith(".wasm") || path.EndsWith(".wasm.gz") || path.EndsWith(".wasm.br"))
                {
                    response.ContentType = "application/wasm";
                }
                else if (path.EndsWith(".js") || path.EndsWith(".js.gz") || path.EndsWith(".js.br"))
                {
                    response.ContentType = "application/javascript";
                }
                else if (path.EndsWith(".data.gz"))
                {
                    response.ContentType = "application/gzip";
                }
                else if (path.EndsWith(".data") || path.EndsWith(".data.br"))
                {
                    response.ContentType = "application/octet-stream";
                }

                string page = rootFolder + path;
                string msg = null;

                // this allows only local access
                if (allowExternalConnections == false && context.Request.IsLocal == false)
                {
                    Tools.Log("Forbidden.", ConsoleColor.Red);
                    msg = "<html><body>403 Forbidden</body></html>";
                    response.StatusCode = 403;
                }
                else if (!File.Exists(page))
                {
                    Tools.Log("Not found: " + page, ConsoleColor.Red);
                    msg = "<html><body>404 Not found</body></html>";
                    response.StatusCode = 404;
                }
                else
                {
                    // display client ip address and request info
                    Tools.Log(context.Request.RemoteEndPoint.Address + " < " + path + (response.ContentType != null ? " (" + response.ContentType + ")" : ""));

                    using (FileStream fileStream = File.Open(page, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (BinaryReader reader = new BinaryReader(fileStream))
                    {
                        response.ContentLength64 = fileStream.Length;

                        byte[] buffer = new byte[4096];
                        int bytesRead;

                        try
                        {
                            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                response.OutputStream.Write(buffer, 0, bytesRead);
                            }
                        }
                        catch (Exception ex)
                        {
                            Tools.Log("Error reading file: " + ex, ConsoleColor.Yellow);
                        }
                    }
                }

                if (msg != null)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(msg);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }

                response.Close();
            }
            catch (Exception)
            {

                throw;
            }
        } // RequestHandler

    } // Program
} // class