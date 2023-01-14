using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
namespace Functions
{
    public partial class Win32
    {
        [LibraryImport("User32.Dll")]
        public static partial void SetCursorPos(int x, int y);

        [LibraryImport("User32.Dll")]
        public static partial void ClientToScreen(IntPtr hWnd, ref POINT point);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;

            public POINT(int X, int Y)
            {
                x = X;
                y = Y;
            }
        }
    }
    public class Mouse
    {
        public static void Move(int moves, int interval)
        {
            Rectangle captureRectangle = Screen.AllScreens[0].Bounds;
            int screenWidth = captureRectangle.Width;
            int screenHeight = captureRectangle.Height;
            for (int i = 0; i < moves; i++)
            {
                Random rand = new();
                Win32.POINT p = new(rand.Next(1, screenWidth), rand.Next(1, screenHeight));
                Win32.SetCursorPos(p.x, p.y);
                Thread.Sleep(interval);
            }
        }
    }
    public class Screenshot
    {
        public static byte[] MakeScreenshot()
        {
            Rectangle captureRectangle = Screen.AllScreens[0].Bounds;
            Bitmap captureBitmap = new(captureRectangle.Width, captureRectangle.Height, PixelFormat.Format32bppArgb);
            Graphics captureGraphics = Graphics.FromImage(captureBitmap);
            captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0, captureRectangle.Size);
            captureBitmap.Save("screenshot.jpeg", ImageFormat.Jpeg);
            byte[] imgData = File.ReadAllBytes(@"screenshot.jpeg");
            return imgData;
        }
    }
    public class Cmd
    {
        public static void Execute(string command)
        {
            Process process = new();
            ProcessStartInfo startInfo = new()
            {
                //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                FileName = "cmd.exe",
                Arguments = $"/C {command}"
            };
            process.StartInfo = startInfo;
            process.Start();
        }
    }
}
