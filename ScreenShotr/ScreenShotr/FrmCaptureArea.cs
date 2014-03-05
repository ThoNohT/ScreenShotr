/********************************************************************************
 Copyright (C) 2012 Eric Bataille <e.c.p.bataille@gmail.com>

 This program is free software; you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation; either version 2 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with this program; if not, write to the Free Software
 Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307, USA.
********************************************************************************/


using System;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ScreenShotr
{
    public partial class FrmCaptureArea : Form
    {
        #region Fields

        // The current capture state.
        private CaptureState state;

        // The positions that were recorded in their respective states.
        private int startX, startY, endX, endY = 0;

        // This stores the initial location of the form so we can revert to this
        // after resizing the form to match the capture area.
        private Point initalLocation;

        // A constant that indicates true in the settings file.
        private const string TRUE = "1";
        
        #region Configuration

        // The key in the configuration file that contains the url to post the upload request to.
        private const string keyUploadUrl = "uploadUrl";

        // The key in the configuration file that contains the password to use when uploading.
        private const string keyUploadPassword = "uploadPassword";

        // The key in the configuration file that contains the http user to use when authorizing.
        private const string keyHttpUser = "httpUser";

        // The key in the configuration file that contains the http passwrod to use when authorizing.
        private const string keyHttpPassword = "httpPassword";

        // The key in the configuration file that is set to TRUE (see the field) if a proxy is to be used.
        // Note that the default windows credentials will be used for the proxy.
        private const string keyUseProxy = "useProxy";

        #endregion Configuration

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="FrmCaptureArea"/> class.
        /// </summary>
        public FrmCaptureArea()
        {
            InitializeComponent();
        }

        #endregion Constructors
        
        #region Methods

        /// <summary>
        /// Executed when the form loads, this starts the mouse hook and sets the capturing state.
        /// </summary>
        private void FrmCaptureArea_Load(object sender, EventArgs e)
        {
            this.initalLocation = this.Location;
            EnableHook();
            this.state = CaptureState.CapturingStart;

        }

        /// <summary>
        /// The mouse hook function, handles the setting of the capture coordinates.
        /// </summary>
        public int MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            MouseHookStruct lParamStruct = (MouseHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookStruct));
            if (nCode < 0) return CallNextHookEx(hHook, nCode, wParam, lParam);

            switch (wParam.ToInt32())
            {
                case WM_LBUTTONDOWN:
                    
                    this.state = CaptureState.CapturingEnd;
                    this.Opacity = 0;
                    lblInfo.Text = "This box shows the area that will be captured.";

                    // Intercept, because this should not move focus to another program.
                    return 1;

                case WM_LBUTTONUP:
                    DisableHook();
                    this.state = CaptureState.Finished;

                    HandleUploading();
                    break;

                case WM_MOUSEMOVE:
                    if (this.state == CaptureState.CapturingStart)
                    {
                        this.startX = lParamStruct.pt.x;
                        this.startY = lParamStruct.pt.y;
                    }
                    if (this.state == CaptureState.CapturingEnd)
                    {
                        this.endX = lParamStruct.pt.x;
                        this.endY = lParamStruct.pt.y;

                        // Calculate the minimum and maximum coordinates.
                        this.Location = new Point(Math.Min(this.startX, this.endX), Math.Min(this.startY, this.endY));
                        var maxLocation = new Point(Math.Max(this.startX, this.endX), Math.Max(this.startY, this.endY));
                        this.Size = new Size(maxLocation.X - this.Location.X, maxLocation.Y - this.Location.Y);

                        this.Opacity = 0.25;
                    }
                    break;

                default:
                    // If the user does anything else, just quit.
                    DisableHook();
                    this.Close();
                    break;
            }

            return CallNextHookEx(hHook, nCode, wParam, lParam);
        }

        /// <summary>
        /// Handles the uploading of the file to the specified service.
        /// </summary>
        private void HandleUploading()
        {
            var result = MessageBox.Show("Upload this area? (cancel retries)", "Upload", MessageBoxButtons.YesNoCancel);

            if (result == DialogResult.Yes)
            {
                // Do the uploading.
                CreateScreenshot();
                this.Close();
            }
            if (result == DialogResult.No || result == DialogResult.None)
            {
                // End.
                this.Close();
            }
            if (result == DialogResult.Cancel)
            {
                // Reinitialize.
                this.FrmCaptureArea_Load(null, null);
            }
        }
        
        /// <summary>
        /// Creates a screenshot and then calls the uploading function.
        /// </summary>
        private void CreateScreenshot()
        {
            // Allow the form to hide itself.
            this.Opacity = 0;
            Thread.Sleep(1000);

            // Get the window handle.
            var hDesk = GetDesktopWindow();
            var hSrce = GetWindowDC(hDesk);
            var hDest = CreateCompatibleDC(hSrce);

            // Create a bitmap.
            var hBmp = CreateCompatibleBitmap(hSrce, this.Size.Width, this.Size.Height);
            var hOldBmp = SelectObject(hDest, hBmp);

            // Try to copy the screen region
            BitBlt(hDest, 0, 0, this.Size.Width, this.Size.Height, hSrce, this.Location.X, this.Location.Y, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
            var bmp = Bitmap.FromHbitmap(hBmp);
            
            // Delete the window handle.
            SelectObject(hDest, hOldBmp);
            DeleteObject(hBmp);
            DeleteDC(hDest);
            ReleaseDC(hDesk, hSrce);

            // Re-show the form.
            this.Opacity = 100;
            this.Location = initalLocation;
            lblInfo.Text = "Uploading...";
            this.ClientSize = new Size(266, 74);
            this.Refresh();

            this.Upload(bmp);
            bmp.Dispose();
        }

        /// <summary>
        /// Uploads the captured image to the location specified in confugiration settings.
        /// </summary>
        /// <param name="bmp">The captured image.</param>
        private void Upload(Bitmap bmp)
        {

            // Get the user settings.
            var url = ConfigurationManager.AppSettings[keyUploadUrl];
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("No url defined, please check your settings.");
                this.Close();
            }
            var httpUser = ConfigurationManager.AppSettings[keyHttpUser];
            var httpPassword = ConfigurationManager.AppSettings[keyHttpPassword];
            var httpLogin = !(string.IsNullOrWhiteSpace(httpUser) || string.IsNullOrWhiteSpace(httpPassword));
            
            // Get the binary data.
            var stream = new MemoryStream();
            bmp.Save(stream, ImageFormat.Png);
            var dataArray = stream.ToArray();
            
            // Create a web request.           
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((a, b, c, d) => true);

            using (var client = new WebClient())
            {
                if (httpLogin) client.Credentials = new NetworkCredential(httpUser, httpPassword);
                else client.UseDefaultCredentials = true;

                if (ConfigurationManager.AppSettings[keyUseProxy] == TRUE)
                    client.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;

                try
                {
                    var response = Encoding.UTF8.GetString(client.UploadData(url, dataArray));

                    if (!response.StartsWith("http"))
                        MessageBox.Show(string.Format("Upload failed ({0}), please check your settings.", response));

                    Clipboard.SetText(response.Trim());
                }
                catch (WebException ex)
                {
                    MessageBox.Show(string.Format("Upload failed ({0}), please check your settings.", ex.Message));
                }
            }
        }

        /// <summary>
        /// Enables the low-level mouse hook.
        /// </summary>
        private void EnableHook()
        {
            // Create an instance of HookProc.
            MouseHookProcedure = new HookProc(MouseHookProc);

            hHook = SetWindowsHookEx(WH_MOUSE_LL, MouseHookProcedure, (IntPtr)0, 0);
            //If the SetWindowsHookEx function fails.
            if (hHook == 0)
            {
                MessageBox.Show("SetWindowsHookEx Failed");
                this.Close();
            }
        }

        /// <summary>
        /// Disables the low-level mouse hook.
        /// </summary>
        private void DisableHook()
        {
            bool ret = UnhookWindowsHookEx(hHook);
            //If the UnhookWindowsHookEx function fails.
            if (ret == false)
            {
                MessageBox.Show("UnhookWindowsHookEx Failed");
                this.Close();
            }
            hHook = 0;
        }

        #endregion Methods
    }
}
