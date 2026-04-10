using System;
using System.Windows.Forms;
using System.Globalization;

namespace QuickBrowser
{
    static class Program
    {
        static Program()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length == 0)
            {
                Application.Run(new BrowserForm());
                return;
            }

            bool kioskMode = false;
            int useScreen = 0;
            float zoom = 1;
            foreach (var arg in args)
                if (arg.Equals("/KIOSKMODE", StringComparison.InvariantCultureIgnoreCase))
                    kioskMode = true;
                else if (arg.StartsWith("/KIOSKMODE:", StringComparison.InvariantCultureIgnoreCase))
                    kioskMode = bool.Parse(arg.Substring("/KIOSKMODE:".Length));
                else if (arg.StartsWith("/SCREEN:", StringComparison.InvariantCultureIgnoreCase))
                    useScreen = int.Parse(arg.Substring("/SCREEN:".Length));
                else if (arg.StartsWith("/ZOOM:", StringComparison.InvariantCultureIgnoreCase))
                    zoom = float.Parse(arg.Substring("/ZOOM:".Length), NumberFormatInfo.InvariantInfo);
                else
                    new BrowserForm(arg, kioskMode, useScreen, zoom).Show();

            Application.Run();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                e.ExceptionObject.GetType().ToString() + Environment.NewLine +
                Environment.NewLine +
                e.ExceptionObject.ToString(),
                "Unhanded exception",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

    }
}
