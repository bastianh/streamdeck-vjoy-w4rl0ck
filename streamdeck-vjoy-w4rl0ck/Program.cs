using BarRaider.SdTools;

namespace streamdeck_vjoy_w4rl0ck;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Uncomment this line of code to wait for debugger on startup
        // while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }
        SDWrapper.Run(args);
    }
}