using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace SimpleWebServer
{
    internal class Program
    {
        static string rootFolder = "";

        static void Main(string[] args)
        {
            string defaultPort = "8080";

            // get local folderpath and port as arguments: SimpleWebServer.exe "C:\Users\user\Documents\My Web Sites\MyWebSite" 8080

            // if too many arguments, show usage
            if (args == null || args.Length > 2 || args.Length < 1)
            {
                Console.WriteLine("Usage: SimpleWebServer.exe [folderpath] [port]");
                Console.ReadLine();
                return;
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
                    return;
                }
            }

            // if no 2 arguments, use current exe folder and port 8080
            if (args.Length < 2)
            {
                args = new string[2];
                args[0] = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                args[1] = defaultPort;
            }

            rootFolder = args[0];
            string port = args[1];

            // check if path exists
            if (!Directory.Exists(rootFolder))
            {
                Console.WriteLine("Path " + rootFolder + " does not exist.");
                Console.ReadLine();
                return;
            }

            // check if tcp listener port is available
            if (!PortAvailable(port))
            {
                Console.WriteLine("Port " + port + " is not available.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Serving directory: " + args[0]);

            string url = "http://localhost:" + port + "/";

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(url);
            Console.WriteLine("Listening for requests on " + url);
            listener.Start();

            listener.BeginGetContext(RequestHandler, listener);

            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }

        private static bool PortAvailable(string port)
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

                // get exe path for now
                //string rootFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                if (path == "/")
                {
                    path = "/index.html";
                }

                if (Path.GetExtension(path) == ".html" || path.EndsWith(".js"))// || path.EndsWith(".js.gz") || path.EndsWith(".js.br"))
                {
                    response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
                    response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
                    response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
                }

                // unity webgl support
                //if (Path.GetExtension(path) == ".gz")
                //{
                //    response.AddHeader("Content-Encoding", "gzip");
                //}
                //else if (Path.GetExtension(path) == ".br")
                //{
                //    response.AddHeader("Content-Encoding", "br");
                //}

                if (context.Request.Headers.Get("Range") != null)
                {
                    response.AddHeader("Accept-Ranges", "bytes");
                }

                //if (path.EndsWith(".wasm") || path.EndsWith(".wasm.gz") || path.EndsWith(".wasm.br"))
                //{
                //    response.ContentType = "application/wasm";
                //}
                else if (path.EndsWith(".js"))// || path.EndsWith(".js.gz") || path.EndsWith(".js.br"))
                {
                    response.ContentType = "application/javascript";
                    Console.WriteLine("js file");
                }
                //else if (path.EndsWith(".data.gz"))
                //{
                //    response.ContentType = "application/gzip";
                //}
                //else if (path.EndsWith(".data") || path.EndsWith(".data.br"))
                //{
                //    response.ContentType = "application/octet-stream";
                //}

                string page = rootFolder + path;
                string msg = null;

                if (!context.Request.IsLocal)
                {
                    Console.WriteLine("Forbidden.");
                    msg = "<html><body>403 Forbidden</body></html>";
                    response.StatusCode = 403;
                }
                else if (!File.Exists(page))
                {
                    Console.WriteLine("Not found");
                    msg = "<html><body>404 Not found</body></html>";
                    response.StatusCode = 404;
                }
                else
                {

                    Console.WriteLine("Serving: " + path + " (" + response.ContentType + ")");


                    FileStream fileStream = File.Open(page, FileMode.Open);
                    BinaryReader reader = new BinaryReader(fileStream);
                    try
                    {
                        response.ContentLength64 = fileStream.Length;
                        byte[] buffer2 = reader.ReadBytes(4096);
                        while (buffer2.Length != 0)
                        {
                            response.OutputStream.Write(buffer2, 0, buffer2.Length);
                            buffer2 = reader.ReadBytes(4096);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error reading file: " + ex);
                    }
                    reader.Close();
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