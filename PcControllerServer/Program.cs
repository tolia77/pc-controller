using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using Functions;

public class Program
{
    static readonly IPHostEntry Host = Dns.GetHostEntry("localhost");
    static readonly IPAddress IpAddress = Host.AddressList[1];

    public static int Main()
    {
        _ = new Server();
        return 0;
    }

    public class Server
    {
        private readonly IPEndPoint testConnectionEndPoint;
        private readonly IPEndPoint moveMouseEndPoint;
        private readonly IPEndPoint sendScreenshotEndPoint;
        private readonly IPEndPoint openLinkEndPoint;
        private readonly IPEndPoint cmdExecuteEndPoint;
        private readonly IPEndPoint shutdownPCEndPoint;
        private readonly IPEndPoint restartPCEndPoint;
        private readonly Thread testConnectionThread;
        private readonly Thread moveMouseThread;
        private readonly Thread sendScreenshotThread;
        private readonly Thread openLinkThread;
        private readonly Thread cmdExecuteThread;
        private readonly Thread shutdownPCThread;
        private readonly Thread restartPCThread;
        public Server()
        {
            testConnectionEndPoint = new(0, 49999);
            moveMouseEndPoint = new(0, 50000);
            sendScreenshotEndPoint = new(0, 50001);
            openLinkEndPoint = new(0, 50002);
            cmdExecuteEndPoint = new(0, 50003);
            shutdownPCEndPoint = new(0, 50004);
            restartPCEndPoint = new(0, 50005);
            testConnectionThread = new(TestConnection);
            moveMouseThread = new(MoveMouse);
            sendScreenshotThread = new(SendScreenshot);
            openLinkThread = new(OpenLink);
            cmdExecuteThread = new(CmdExecute);
            shutdownPCThread = new(ShutdownPC);
            restartPCThread = new(RestartPC);
            testConnectionThread.Start();
            moveMouseThread.Start();
            sendScreenshotThread.Start();
            openLinkThread.Start();
            cmdExecuteThread.Start();
            shutdownPCThread.Start();
            restartPCThread.Start();
            using (Socket sender = new(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                sender.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = sender.LocalEndPoint as IPEndPoint;
                Console.WriteLine("Your local ip: " + endPoint.Address.ToString());
            }
            Console.WriteLine("Server started");
        }

        private void TestConnection()
        {
            Socket listener = new(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(testConnectionEndPoint);
            listener.Listen(10);
            while (true)
            {
                Socket handler = listener.Accept();
                byte[] msg = Encoding.UTF8.GetBytes("SUCCESS");
                handler.Send(msg);
                handler.Close();
            }
        }

        private void MoveMouse()
        {
            Socket listener = new(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(moveMouseEndPoint);
            listener.Listen(10);
            while (true)
            {
                Socket handler = listener.Accept();
                Console.WriteLine("Connected");
                string data = "";
                byte[] bytes = new byte[1024];
                try
                {
                    int bytesRec = handler.Receive(bytes);
                    data += Encoding.UTF8.GetString(bytes, 0, bytesRec);
                    string[] dataArr = new string[2];
                    dataArr = data.Split(' ');
                    int moves = Convert.ToInt32(dataArr[0]);
                    int interval = Convert.ToInt32(dataArr[1]);
                    if (data == "break") break;
                    Thread moveThread = new(() => Mouse.Move(moves, interval));
                    moveThread.Start();
                    byte[] msg = Encoding.UTF8.GetBytes("SUCCESS");
                    handler.Send(msg);
                }
                catch (SocketException)
                {
                    handler.Shutdown(SocketShutdown.Both);
                    Console.WriteLine("ERROR: Connection aborted");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: {e}");
                }
                handler.Close();
            }
        }

        private void SendScreenshot()
        {
            Socket listener = new(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(sendScreenshotEndPoint);
            listener.Listen(10);
            while (true)
            {
                Socket handler = listener.Accept();
                Console.WriteLine("Connected");
                try
                {
                    byte[] buffer = Screenshot.MakeScreenshot();
                    int v = handler.Send(buffer, buffer.Length, SocketFlags.None);
                }
                catch (SocketException)
                {
                    handler.Shutdown(SocketShutdown.Both);
                    Console.WriteLine("Connection aborted");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: {e}");
                }
                handler.Close();
            }
        }

        private void OpenLink()
        {
            Socket listener = new(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(openLinkEndPoint);
            listener.Listen(10);
            while (true)
            {
                Socket handler = listener.Accept();
                Console.WriteLine("Connected");
                string data = "";
                byte[] bytes = new byte[1024];
                try
                {
                    int bytesRec = handler.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    Process.Start(data);
                    handler.Send(Encoding.ASCII.GetBytes("SUCCESS"));
                }
                catch (SocketException)
                {
                    handler.Shutdown(SocketShutdown.Both);
                    Console.WriteLine("Connection aborted");
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    Console.WriteLine("ERROR");
                    handler.Send(Encoding.ASCII.GetBytes("ERROR"));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: {e}");
                }
                handler.Close();
            }
        }

        private void CmdExecute()
        {
            Socket listener = new(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(cmdExecuteEndPoint);
            listener.Listen(10);
            while (true)
            {
                Socket handler = listener.Accept();
                Console.WriteLine("Connected");
                string data = "";
                byte[] bytes = new byte[1024];
                try
                {
                    int bytesRec = handler.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    Cmd.Execute(data);
                    handler.Send(Encoding.ASCII.GetBytes("SUCCESS"));
                }
                catch (SocketException)
                {
                    handler.Shutdown(SocketShutdown.Both);
                    Console.WriteLine("Connection aborted");
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    Console.WriteLine("ERROR");
                    handler.Send(Encoding.ASCII.GetBytes("ERROR"));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: {e}");
                }
                handler.Close();
            }
        }

        private void ShutdownPC()
        {
            Socket listener = new(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(shutdownPCEndPoint);
            listener.Listen(10);
            while (true)
            {
                Socket handler = listener.Accept();
                Process.Start("ShutDown", "/s /t 0");
                handler.Close();
            }
        }

        private void RestartPC()
        {
            Socket listener = new(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(restartPCEndPoint);
            listener.Listen(10);
            while (true)
            {
                Socket handler = listener.Accept();
                Process.Start("ShutDown", "/r /t 0");
                handler.Close();
            }
        }
    }
}