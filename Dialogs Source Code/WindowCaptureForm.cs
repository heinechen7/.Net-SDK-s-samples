﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WindowCaptureForm.cs" company="VisioForge">
//   VisioForge (c) 2006 - 2021
// </copyright>
// <summary>
//   Defines the WindowCaptureForm type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace VisioForge.Controls.UI.Dialogs.Shared
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Windows.Forms;

    using VisioForge.Shared;
    using VisioForge.Types;

    /// <summary>
    /// The window capture form.
    /// </summary>
    public partial class WindowCaptureForm : Form
    {
        /// <summary>
        /// Gets or sets a value indicating whether window moved by mouse button down.
        /// </summary>
        public bool MoveByMouseDown { get; set; } = true;

        /// <summary>
        /// Gets or sets default border color.
        /// </summary>
        public Color DefaultBorderColor { get; set; } = Color.Red;

        /// <summary>
        /// Gets info label.
        /// </summary>
        public Label InfoLabel => lbInfo;

        /// <summary>
        /// OnCaptureHotkey event.
        /// </summary>
        public event EventHandler<WindowCaptureEventArgs> OnCaptureHotkey; 

        /// <summary>
        /// Gets capture window text.
        /// </summary>
        public string CapturedWindowText { get; private set; }

        /// <summary>
        /// Gets captured window handle.
        /// </summary>
        public IntPtr CapturedWindowHandle { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowCaptureForm"/> class.
        /// </summary>
        public WindowCaptureForm()
        {
            InitializeComponent();

            UpdateBackgroundImage();
            this.FormBorderStyle = FormBorderStyle.None;
            this.Width = this.BackgroundImage.Width;
            this.Height = this.BackgroundImage.Height;
            this.TransparencyKey = Color.Fuchsia;

            // Hooks only into specified Keys (here "A" and "B").
            _globalKeyboardHook = new GlobalKeyboardHook(new Keys[] { Keys.W, Keys.LControlKey, Keys.RControlKey, Keys.RShiftKey, Keys.LShiftKey });

            // Hooks into all keys.
            //_globalKeyboardHook = new GlobalKeyboardHook();
            _globalKeyboardHook.KeyboardPressed += OnKeyPressed;
        }

        #region Hotkey

        private bool _ctrlPressed;

        private bool _shiftPressed;

        private GlobalKeyboardHook _globalKeyboardHook;

        private void OnKeyPressed(object sender, GlobalKeyboardHookEventArgs e)
        {
            // EDT: No need to filter for VkSnapshot anymore. This now gets handled
            // through the constructor of GlobalKeyboardHook(...).
            if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown)
            {
                var key = e.KeyboardData.Key;
                if (key == Keys.LControlKey || key == Keys.RControlKey)
                {
                    _ctrlPressed = true;
                }
                else if (key == Keys.LShiftKey || key == Keys.RShiftKey)
                {
                    _shiftPressed = true;
                }
                else if (key == Keys.W && _shiftPressed && _ctrlPressed)
                {
                    OnCaptureHotkey?.Invoke(this, new WindowCaptureEventArgs(CapturedWindowText, this.CapturedWindowHandle));
                }
            }
            else if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyUp)
            {
                var key = e.KeyboardData.Key;
                if (key == Keys.LControlKey || key == Keys.RControlKey)
                {
                    _ctrlPressed = false;
                }
                else if (key == Keys.LShiftKey || key == Keys.RShiftKey)
                {
                    _shiftPressed = false;
                }
            }
        }

        #endregion

        private void UpdateBackgroundImage()
        {
            var bmp = new Bitmap(
                Width,
                Height,
                PixelFormat.Format32bppArgb);

            using (var grf = Graphics.FromImage(bmp))
            {
                grf.Clear(Color.Fuchsia); 

                var pen = new Pen(DefaultBorderColor, 3f);

                //pen.Alignment = PenAlignment.Inset;

                var rect = new Rectangle(0, 0, Width, Height);
                grf.DrawRectangle(pen, rect);
            }

            if (BackgroundImage != null)
            {
                BackgroundImage.Dispose();
                BackgroundImage = null;
            }

            BackgroundImage = bmp;
        }

        #region Mouse

        private Point lastPoint;

        private void WindowCaptureForm_MouseDown(object sender, MouseEventArgs e)
        {
            lastPoint = new Point(e.X, e.Y);
        }

        private void WindowCaptureForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (MoveByMouseDown && e.Button == MouseButtons.Left)
            {
                this.Left += e.X - lastPoint.X;
                this.Top += e.Y - lastPoint.Y;
            }
        }

        #endregion

        /// <summary>
        /// Updates location.
        /// </summary>
        /// <param name="rect">
        /// Rectangle.
        /// </param>
        public void UpdateLocation(Rectangle rect)
        {
            Bounds = rect;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.CapturedWindowHandle = WindowCaptureAPI.GetWindowOnCursor();
            if (this.CapturedWindowHandle != IntPtr.Zero)
            {
                CapturedWindowText = WindowCaptureAPI.GetWindowText(this.CapturedWindowHandle);
                var res = WindowCaptureAPI.GetWindowRect(this.CapturedWindowHandle, out Rectangle rect);

                if (res)
                {
                    UpdateLocation(rect);
                    Show();
                }
                else
                {
                    Hide();
                }
            }
            else
            {
                Hide();
            }
        }

        /// <summary>
        /// Starts capture.
        /// </summary>
        public void StartCapture()
        {
            timer1.Start();
        }

        /// <summary>
        /// Stops capture.
        /// </summary>
        public void StopCapture()
        {
            timer1.Stop();
        }
    }
}
