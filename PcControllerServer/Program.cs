using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using Functions;
using SocketServer;

internal class Program
{
    private static int Main()
    {
        _ = new Server();
        return 0;
    }
}

namespace SocketServer
{
    public class Server
    {
        public static Socket DefaultSocket
        {
            get => new(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            set { }
        }
        private static readonly IPHostEntry Host = Dns.GetHostEntry("localhost");
        private static readonly IPAddress IpAddress = Host.AddressList[1];
        public IPEndPoint TestConnectionEndPoint { get; set; } = new(0, 39999);
        public IPEndPoint MoveMouseEndPoint { get; set; } = new(0, 40000);
        public IPEndPoint SendScreenshotEndPoint { get; set; } = new(0, 40001);
        public IPEndPoint OpenLinkEndPoint { get; set; } = new(0, 40002);
        public IPEndPoint CmdExecuteEndPoint { get; set; } = new(0, 40003);
        public IPEndPoint ShutdownPCEndPoint { get; set; } = new(0, 40004);
        public IPEndPoint RestartPCEndPoint { get; set; } = new(0, 40005);
        public IPEndPoint StreamScreenEndPoint { get; set; } = new(0, 40006);
        private readonly Thread testConnectionThread;
        private readonly Thread moveMouseThread;
        private readonly Thread sendScreenshotThread;
        private readonly Thread openLinkThread;
        private readonly Thread cmdExecuteThread;
        private readonly Thread shutdownPcThread;
        private readonly Thread restartPcThread;
        private readonly Thread streamScreenThread;
        public Server()
        {
            try
            {
                Console.WriteLine("Your local IP: " + GetLocalIP().ToString());
                testConnectionThread = new(TestConnectionListener);
                moveMouseThread = new(MoveMouseListener);
                sendScreenshotThread = new(SendScreenshotListener);
                openLinkThread = new(OpenLinkListener);
                cmdExecuteThread = new(CmdExecuteListener);
                shutdownPcThread = new(ShutdownPCListener);
                restartPcThread = new(RestartPCListener);
                streamScreenThread = new(StreamScreenListener);
                testConnectionThread.Start();
                moveMouseThread.Start();
                sendScreenshotThread.Start();
                openLinkThread.Start();
                cmdExecuteThread.Start();
                shutdownPcThread.Start();
                restartPcThread.Start();
                streamScreenThread.Start();
                Console.WriteLine("Server started");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        public static IPAddress GetLocalIP()
        {
            using Socket socketSender = new(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socketSender.Connect("8.8.8.8", 65530);
            if (socketSender.LocalEndPoint is not IPEndPoint endPoint) throw new SocketException();
            return endPoint.Address;
        }

        public void TestConnectionListener()
        {
            Socket listener = DefaultSocket;
            listener.Bind(TestConnectionEndPoint);
            listener.Listen(10);
            while (true)
            {
                Socket handler = listener.Accept();
                byte[] msg = Encoding.UTF8.GetBytes("SUCCESS");
                handler.Send(msg);
                handler.Close();
            }
        }

        public void MoveMouseListener()
        {
            Socket listener = DefaultSocket;
            listener.Bind(MoveMouseEndPoint);
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
                    int repeats = Convert.ToInt32(data.Split()[0]);
                    int interval = Convert.ToInt32(data.Split()[1]);
                    Thread moveThread = new(() => Mouse.ChaoticMove(repeats, interval));
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

        public void SendScreenshotListener()
        {
            Socket listener = DefaultSocket;
            listener.Bind(SendScreenshotEndPoint);
            listener.Listen(10);
            while (true)
            {
                Socket handler = listener.Accept();
                Console.WriteLine("Connected");
                try
                {
                    byte[] buffer = Screenshot.GetScreenshot();
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

        public void StreamScreenListener()
        {
            Socket listener = DefaultSocket;
            listener.Bind(StreamScreenEndPoint);
            listener.Listen(10);
            while (true)
            {
                Socket handler = listener.Accept();
                Console.WriteLine("Connected");
                while (handler.Connected)
                {
                    try
                    {
                        byte[] buffer = Screenshot.GetScreenshot();
                        handler.Send(BitConverter.GetBytes(buffer.Length));
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
                }
                handler.Close();
            }
        }

        public void OpenLinkListener()
        {
            Socket listener = DefaultSocket;
            listener.Bind(OpenLinkEndPoint);
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
                    Cmd.OpenLink(data);
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

        public void CmdExecuteListener()
        {
            Socket listener = DefaultSocket;
            listener.Bind(CmdExecuteEndPoint);
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

        public void ShutdownPCListener()
        {
            Socket listener = DefaultSocket;
            listener.Bind(ShutdownPCEndPoint);
            listener.Listen(10);
            while (true)
            {
                Socket handler = listener.Accept();
                Process.Start("ShutDown", "/s /t 0");
                handler.Close();
            }
        }

        public void RestartPCListener()
        {
            Socket listener = DefaultSocket;
            listener.Bind(RestartPCEndPoint);
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