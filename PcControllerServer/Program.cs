using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using Functions;

namespace SocketServer
{
    class Program
    {
        public static int Main()
        {
            _ = new Server();
            return 0;
        }
    }
    public class Server
    {
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
                using (Socket socketSender = new(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socketSender.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socketSender.LocalEndPoint as IPEndPoint;
                    Console.WriteLine("Your local ip: " + endPoint.Address.ToString());
                }
                testConnectionThread = new(TestConnection);
                moveMouseThread = new(MoveMouse);
                sendScreenshotThread = new(SendScreenshot);
                openLinkThread = new(OpenLink);
                cmdExecuteThread = new(CmdExecute);
                shutdownPcThread = new(ShutdownPC);
                restartPcThread = new(RestartPC);
                streamScreenThread = new(StreamScreen);
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

        private void TestConnection()
        {
            Socket listener = new(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
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

        private void MoveMouse()
        {
            Socket listener = new(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
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
            listener.Bind(SendScreenshotEndPoint);
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

        private void StreamScreen()
        {
            Socket listener = new(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
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
                        byte[] buffer = Screenshot.MakeScreenshot();
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

        private void OpenLink()
        {
            Socket listener = new(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
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

        private void CmdExecute()
        {
            Socket listener = new(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
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

        private void ShutdownPC()
        {
            Socket listener = new(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(ShutdownPCEndPoint);
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