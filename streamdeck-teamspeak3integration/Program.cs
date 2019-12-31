using BarRaider.SdTools;

namespace ZerGo0.TeamSpeak3Integration
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Uncomment this line of code to allow for debugging
            //while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }

            SDWrapper.Run(args);
        }
    }
}