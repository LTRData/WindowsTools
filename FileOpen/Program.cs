using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace FileOpen
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var openFileDialog = new OpenFileDialog()
            {
                AutoUpgradeEnabled = true,
                CheckFileExists = true,
                CheckPathExists = true,
                ShowReadOnly = false,
                DereferenceLinks = true,
                InitialDirectory = Environment.CurrentDirectory,
                Multiselect = true,
                RestoreDirectory = false,
                SupportMultiDottedExtensions = true
            };

            while (openFileDialog.ShowDialog() == DialogResult.OK)
                foreach (var file in openFileDialog.FileNames)
                    Process.Start(file);
        }
    }
}
