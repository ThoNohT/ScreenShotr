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

        // A string containing a null character in order to be able to separate the password from the actual image.
        private string nullStr = Convert.ToChar(0x0).ToString();

        #region Configuration

        // The key in the configuration file that contains the url to post the upload request to.
        private const string keyUploadUrl = "uploadUrl";

        // The key in the configuration file that contains the password to use when uploading.
        private const string keyUploadPassword = "uploadPassword";

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

            this.Upload(bmp);
            bmp.Dispose();
        }

        private void Upload(Bitmap bmp)
        {
            var stream = new MemoryStream();
            bmp.Save(stream, ImageFormat.Png);

            // Get the user settings.
            var pass = ConfigurationManager.AppSettings[keyUploadPassword];
            if (pass == null)
            {
                MessageBox.Show("No password defined, please check your settings.");
                this.Close();
            }
            var url = ConfigurationManager.AppSettings[keyUploadUrl];
            if (url == null)
            {
                MessageBox.Show("No url defined, please check your settings.");
                this.Close();
            }

            // Create a web request.
            var passArray = Encoding.ASCII.GetBytes(pass + nullStr);
            var array = stream.ToArray();
            var request = WebRequest.Create(url);
            request.Method = "POST";
            request.ContentLength = array.Length + passArray.Length;
            request.ContentType = "application/x-httpd-php";

            // Fill the data of the request.
            var dataStream = request.GetRequestStream();
            dataStream.Write(passArray, 0, passArray.Length);
            dataStream.Write(array, 0, array.Length);
            dataStream.Close();

            // Get the response.
            var response = request.GetResponse();
            var status = ((HttpWebResponse)response).StatusDescription;

            if (status == "OK")
            {
                dataStream = response.GetResponseStream();
                var reader = new StreamReader(dataStream);
                string responseString = reader.ReadToEnd().Trim();

                if (!responseString.StartsWith("http"))
                    MessageBox.Show(string.Format("Upload failed ({0}), please check your settings.", responseString));

                Clipboard.SetText(responseString.Trim());
            }
            else
            {
                MessageBox.Show("Upload failed, please check your settings.");
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
