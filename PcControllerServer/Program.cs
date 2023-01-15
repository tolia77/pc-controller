using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using Functions;

class Program
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
        private readonly IPEndPoint testConnectionEndPoint = new(0, 49999);
        private readonly IPEndPoint moveMouseEndPoint = new(0, 50000);
        private readonly IPEndPoint sendScreenshotEndPoint = new(0, 50001);
        private readonly IPEndPoint openLinkEndPoint = new(0, 50002);
        private readonly IPEndPoint cmdExecuteEndPoint = new(0, 50003);
        private readonly IPEndPoint shutdownPCEndPoint = new(0, 50004);
        private readonly IPEndPoint restartPCEndPoint = new(0, 50005);
        private readonly Thread testConnectionThread;
        private readonly Thread moveMouseThread;
        private readonly Thread sendScreenshotThread;
        private readonly Thread openLinkThread;
        private readonly Thread cmdExecuteThread;
        private readonly Thread shutdownPcThread;
        private readonly Thread restartPcThread;
        public Server()
        {
            testConnectionThread = new(TestConnection);
            moveMouseThread = new(MoveMouse);
            sendScreenshotThread = new(SendScreenshot);
            openLinkThread = new(OpenLink);
            cmdExecuteThread = new(CmdExecute);
            shutdownPcThread = new(ShutdownPC);
            restartPcThread = new(RestartPC);
            testConnectionThread.Start();
            moveMouseThread.Start();
            sendScreenshotThread.Start();
            openLinkThread.Start();
            cmdExecuteThread.Start();
            shutdownPcThread.Start();
            restartPcThread.Start();
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
                byte[] bytes = new byte[1024];
                try
                {
                    handler.Receive(bytes);
                    string data = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    int moves = Convert.ToInt32(data.Split()[0]);
                    int interval = Convert.ToInt32(data.Split()[1]);
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
                    byte[] msg = Encoding.UTF8.GetBytes("ERROR");
                    handler.Send(msg);
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
                    handler.Send(buffer, buffer.Length, SocketFlags.None);
                }
                catch (SocketException)
                {
                    handler.Shutdown(SocketShutdown.Both);
                    Console.WriteLine("Connection aborted");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: {e}");
                    byte[] msg = Encoding.UTF8.GetBytes("ERROR");
                    handler.Send(msg);
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
                byte[] bytes = new byte[1024];
                try
                {
                    handler.Receive(bytes);
                    string data = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
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
                    byte[] msg = Encoding.UTF8.GetBytes("ERROR");
                    handler.Send(msg);
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
                byte[] bytes = new byte[1024];
                try
                {
                    handler.Receive(bytes);
                    string data = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
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
                    byte[] msg = Encoding.UTF8.GetBytes("ERROR");
                    handler.Send(msg);
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