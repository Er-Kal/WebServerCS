using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;

namespace Server{
    public class Server
    {
        private static TcpListener listener;
        private static int port = 8080;
        private static IPAddress address;

        private static string currentDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
        private static IPAddress GetLocalIP() {
            string localIP = Dns.GetHostAddresses("")[1].MapToIPv4().ToString();
            return IPAddress.Parse(localIP);
        }
        static void Main(string[] args)
        {
            Console.WriteLine(currentDirectory);
            address = GetLocalIP();
            listener = new TcpListener(address, port);
            listener.Start();
            Console.WriteLine($"Server running on local address {address}:{port}");
            Thread thread = new Thread(new ThreadStart(Listen));
            thread.Start();
        }
        private static void Listen()
        {
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                byte[] reqBytes = new byte[1024];
                stream.Read(reqBytes, 0, reqBytes.Length);
                string request = Encoding.UTF8.GetString(reqBytes,0,reqBytes.Length);
                (string firstLine, Dictionary<string, string>) requestData = ParseHeaders(request);
                Console.WriteLine(requestData.Item1);
               
                string contentEncoding = requestData.Item2.GetValueOrDefault("Accept-Encoding");

                if (requestData.firstLine.StartsWith("GET"))
                {
                    string[] flSplit = requestData.firstLine.Split(" ");
                    byte[] file = SendContent(flSplit[1]);
                    if ((flSplit[1].EndsWith("html") || flSplit[1]=="/")  && file != null)
                    {
                        SendHTML(ref stream);
                        stream.Write(file, 0, file.Length);
                    }
                    else if (flSplit[1].EndsWith("css") && file != null)
                    {
                        SendCSS(ref stream);
                        stream.Write(file,0,file.Length);
                    }
                    else
                    {
                        SendHeaders(404, "Page not found", contentEncoding, 0, ref stream);
                    }
                }
                else
                {
                    SendHeaders(405, "Method Disallowed", contentEncoding,0, ref stream);
                }
                client.Close();
            }
        }
        private static (string firstLine,Dictionary<string,string>) ParseHeaders(string headers)
        {
            string[] data = headers.Split('\n');
            string requestType = data[0];
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (string s in data)
            {
                if (s.IndexOf(":") > 0)
                {
                    string[] line = s.Trim().Split(": ");
                    dict.Add(line[0], line[1]);
                }
            }
            return (requestType, dict);
        }
        private static void SendHTML(ref NetworkStream ns)
        {
            string headers = "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n"+"\r\n";
            byte[] responseByte = Encoding.UTF8.GetBytes(headers);
            ns.Write(responseByte, 0, responseByte.Length);
        }
        private static void SendCSS(ref NetworkStream ns)
        {
            string headers = "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/css; charset=UTF-8\r\n" + "\r\n";
            byte[] responseByte = Encoding.UTF8.GetBytes(headers);
            ns.Write(responseByte, 0, responseByte.Length);
        }

        private static void SendHeaders(int statusCode, string statusMsg, string? contentEncoding,int len, ref NetworkStream ns ) {
            string headers =
                $"HTTP/1.1 {statusCode} {statusMsg}\r\n" +
                "Connection: Keep-alive\r\n" +
                $"Content-Encoding: {contentEncoding}\r\n" +
                "Content-Type: application/signed-exchange;v=b3\r\n" +
                $"Date: {DateTime.Today.ToString()}\r\n"+
                "Server: PC\r\n"
                + "\r\n"
                ;
            byte[] responseByte = Encoding.UTF8.GetBytes(headers);
            ns.Write(responseByte, 0, responseByte.Length);
        }
        private static byte[] SendContent(string path)
        {
            if (path == "/") {
                path = "index.html";
            }
            string filePath = Path.Join(currentDirectory, path);
            if (File.Exists(filePath))
            {
                byte[] file = File.ReadAllBytes(filePath);
                return file;
            }
            return null;
        }
    }
}
