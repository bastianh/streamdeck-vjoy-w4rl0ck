using BarRaider.SdTools;

namespace dev.w4rl0ck.streamdeck.vjoy
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Uncomment this line of code to wait for debugger on startup
            // while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }
            SDWrapper.Run(args);
        }
    }
}