using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace QuickBrowser
{
    public partial class BrowserForm : Form
    {
        private readonly string startUrl;
        private readonly bool startKioskMode;
        private readonly float startZoom = 1;

        public BrowserForm(string url, bool kioskMode, int useScreen, float zoom)
        {
            InitializeComponent();

            startUrl = url;
            startKioskMode = kioskMode;
            startZoom = zoom;

            if (useScreen != 0)
                Bounds = Screen.AllScreens[useScreen - 1].Bounds;
        }

        public BrowserForm()
        {
            InitializeComponent();

            var screenSize = Screen.FromControl(this).Bounds.Size;
            Height = screenSize.Height * 3 / 4;
            Width = screenSize.Width * 3 / 4;
            CenterToScreen();
        }

        private void tbUrl_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    e.SuppressKeyPress = true;
                    webBrowser.Navigate(tbUrl.Text);
                    break;

                case Keys.F5:
                    webBrowser.Refresh();
                    break;

                default:
                    if (GlobalKeyDownHandler(e.KeyCode))
                        e.SuppressKeyPress = true;
                    break;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Scale(new SizeF(startZoom, startZoom));

            if (startKioskMode)
                KioskMode();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            ResetText();

            if (startUrl != null)
                webBrowser.Navigate(startUrl);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);

            using (
                var frmProp = new Form()
                {
                    Text = "Browser properties"
                })
            {
                var prop = new PropertyGrid()
                {
                    SelectedObject = webBrowser,
                    Dock = DockStyle.Fill
                };
                frmProp.Controls.Add(prop);
                frmProp.ShowDialog(this);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (Application.OpenForms.Count <= 1)
                Application.Exit();
        }

        private void webBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            pbStatus.BackColor = Color.DarkRed;
        }

        public override void ResetText()
        {
            Text = "QuickBrowser (using Internet Explorer " + webBrowser.Version + ")";
        }

        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            pbStatus.BackColor = Color.LightGreen;
            tbUrl.Text = webBrowser.Url.OriginalString;
            var docTitle = webBrowser.DocumentTitle;
            if (string.IsNullOrEmpty(docTitle))
                ResetText();
            else
                Text = docTitle.Trim();
            if (tbUrl.Visible)
                tbUrl.Focus();
        }

        private bool GlobalKeyDownHandler(Keys keycode)
        {
            switch (keycode)
            {
                case Keys.F11:
                    KioskMode();
                    return true;

                default:
                    return false;
            }
        }

        private void KioskMode()
        {
            if (FormBorderStyle == FormBorderStyle.None)
            {
                tbUrl.Visible = true;
                pbStatus.Visible = true;
                toolStripContainer.BottomToolStripPanelVisible = true;
                webBrowser.Dock = DockStyle.None;
                webBrowser.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
                webBrowser.Size =
                    new Size(
                        webBrowser.Parent.Width - webBrowser.Left,
                        webBrowser.Parent.Height - webBrowser.Top);

                webBrowser.ScrollBarsEnabled = true;
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.Sizable;
                tbUrl.Focus();
            }
            else
            {
                tbUrl.Visible = false;
                pbStatus.Visible = false;
                toolStripContainer.BottomToolStripPanelVisible = false;
                webBrowser.Dock = DockStyle.Fill;
                webBrowser.ScrollBarsEnabled = false;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                webBrowser.Focus();
            }
        }

        private void webBrowser_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (webBrowser.Focused)
                GlobalKeyDownHandler(e.KeyCode);
        }

        private void webBrowser_ProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
        {
            try
            {
                unchecked
                {
                    progressBar.Maximum = (int)(e.MaximumProgress >= e.CurrentProgress ? e.MaximumProgress : e.CurrentProgress);
                    progressBar.Value = (int)(e.CurrentProgress >= progressBar.Minimum ? e.CurrentProgress : 0);
                }
            }
            catch (Exception)
            {
            }
        }

        void webBrowser_StatusTextChanged(object sender, System.EventArgs e)
        {
            lblStatus.Text = webBrowser.StatusText;
        }

        private void webBrowser_NewWindow(object sender, CancelEventArgs e)
        {
            if (Debugger.IsAttached)
                Debugger.Break();
        }
    }
}
