﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Form1.cs" company="VisioForge">
//   Computer Vision demo.
// </copyright>
// <summary>
//   Defines the Form1 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Computer_Vision_Demo
{
    using System;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;

    using VisioForge.Core.CV;
    using VisioForge.Core.MediaPlayer;
    using VisioForge.Core.VideoCapture;
    using VisioForge.MediaFramework.MFP;
    using VisioForge.Types;
    using VisioForge.Types.Events;
    using VisioForge.Types.MediaPlayer;
    using VisioForge.Types.VideoCapture;
    using VisioForge.Types.VideoProcessing;

    public partial class Form1 : Form
    {
        private VideoCaptureCore VideoCapture1;

        private MediaPlayerCore MediaPlayer1;

        private FaceDetector faceDetector;

        private CarCounter carCounter;

        private PedestrianDetector pedestrianDetector;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            VideoCapture1 = new VideoCaptureCore(VideoCaptureView as IVideoView);
            VideoCapture1.OnVideoFrameBuffer += VideoCapture1_OnVideoFrameBuffer;
            VideoCapture1.OnError += VideoCapture1_OnError;

            MediaPlayer1 = new MediaPlayerCore(MediaPlayerView as IVideoView);
            MediaPlayer1.OnVideoFrameBuffer += MediaPlayer1_OnVideoFrameBuffer;
            MediaPlayer1.OnError += MediaPlayer1_OnError;

            Text += " (SDK v" + VideoCapture1.SDK_Version + ")";

            foreach (var device in VideoCapture1.Video_CaptureDevices)
            {
                cbVideoInputDevice.Items.Add(device.Name);
            }

            if (cbVideoInputDevice.Items.Count > 0)
            {
                cbVideoInputDevice.SelectedIndex = 0;
                cbVideoInputDevice_SelectedIndexChanged(null, null);
            }

            cbIPCameraType.SelectedIndex = 0;
        }

        private void MediaPlayer1_OnError(object sender, ErrorsEventArgs e)
        {
            Log(e.Message);
        }

        #region Face detection

        private void FaceDetectionAdd()
        {
            faceDetector = new FaceDetector();
            faceDetector.OnFaceDetected += OnFaceDetected;

            FaceDetectionUpdate();
        }

        private void FaceDetectionUpdate()
        {
            faceDetector.DrawEnabled = cbFDDraw.Checked;
            faceDetector.DrawColor = Color.Green;
            faceDetector.FramesToSkip = tbFDSkipFrames.Value;
            faceDetector.MinNeighbors = tbFDMinNeighbors.Value;
            faceDetector.ScaleFactor = tbFDScaleFactor.Value / 100.0f;
            faceDetector.VideoScale = tbFDDownscale.Value / 10.0f;
            
            if (rbFDCircle.Checked)
            {
                faceDetector.DrawShapeType = CVShapeType.Circle;
            }
            else
            {
                faceDetector.DrawShapeType = CVShapeType.Rectangle;
            }

            faceDetector.MinFaceSize = new Size(Convert.ToInt32(edFDMinFaceWidth.Text), Convert.ToInt32(edFDMinFaceHeight.Text));

            var path = Path.GetDirectoryName(Application.ExecutablePath);
            string facePath = cbFDFace.Checked ? Path.Combine(path, "haarcascade_frontalface_default.xml") : null;
            string eyesPath = cbFDEyes.Checked ? Path.Combine(path, "haarcascade_eye.xml") : null;
            string nosePath = cbFDNose.Checked ? Path.Combine(path, "haarcascade_mcs_nose.xml") : null;
            string mouthPath = cbFDMouth.Checked ? Path.Combine(path, "haarcascade_mcs_mouth.xml") : null;

            faceDetector.Init(
                facePath,
                eyesPath,
                nosePath,
                mouthPath,
                true);

            faceDetector.UpdateSettings();
        }

        private void btFDUpdate_Click(object sender, EventArgs e)
        {
            FaceDetectionUpdate();
        }

        private void FaceDetectionRemove()
        {
            if (this.faceDetector != null)
            {
                this.faceDetector.OnFaceDetected -= OnFaceDetected;
                this.faceDetector.Dispose();
                this.faceDetector = null;
            }
        }

        private void OnFaceDetected(object sender, OnCVFaceDetectedEventArgs e)
        {
            if (e.Faces.Length == 0)
            {
                return;
            }

            BeginInvoke(
                (Action)(() =>
                {
                    edFDFaces.Text = string.Empty;
                    foreach (var face in e.Faces)
                    {
                        edFDFaces.Text += $"Face at {face.Position.ToNiceString()}" + Environment.NewLine;
                    }
                }));
        }

        private void tbFDSkipFrames_Scroll(object sender, EventArgs e)
        {
            lbFDSkipFrames.Text = tbFDSkipFrames.Value.ToString();
        }

        private void tbFDDownscale_Scroll(object sender, EventArgs e)
        {
            lbFDDownscale.Text = (tbFDDownscale.Value / 10.0).ToString("F2");
        }

        private void tbMinNeighbors_Scroll(object sender, EventArgs e)
        {
            lbFDMinNeighbors.Text = tbFDMinNeighbors.Value.ToString();
        }

        private void tbScaleFactor_Scroll(object sender, EventArgs e)
        {
            lbFDScaleFactor.Text = (tbFDScaleFactor.Value / 100.0).ToString("F2");
        }

        #endregion

        #region Pedestrian detection
        private void PedestrianDetectionAdd()
        {
            pedestrianDetector = new PedestrianDetector()
            {
                DrawEnabled = cbPDDraw.Checked,
                DrawColor = Color.Green,
                FramesToSkip = tbPDSkipFrames.Value,
                VideoScale = tbPDDownscale.Value / 10.0f
            };

            pedestrianDetector.Init();

            pedestrianDetector.OnPedestrianDetected += OnPedestrianDetected;
        }

        private void PedestrianDetectionRemove()
        {
            if (pedestrianDetector != null)
            {
                pedestrianDetector.OnPedestrianDetected -= OnPedestrianDetected;
                pedestrianDetector.Dispose();
                pedestrianDetector = null;
            }
        }

        private void OnPedestrianDetected(object sender, OnCVPedestrianDetectedEventArgs e)
        {
            if (e.Items.Length == 0)
            {
                return;
            }

            BeginInvoke(
                (Action)(() =>
                {
                    edPDDetected.Text = string.Empty;
                    foreach (var item in e.Items)
                    {
                        edPDDetected.Text += $"Object at {item.ToNiceString()}" + Environment.NewLine;
                    }
                }));
        }

        private void tbPDDownscale_Scroll(object sender, EventArgs e)
        {
            lbPDDownscale.Text = (tbPDDownscale.Value / 10.0).ToString("F2");
        }

        private void tbPDSkipFrames_Scroll(object sender, EventArgs e)
        {
            lbPDSkipFrames.Text = tbPDSkipFrames.Value.ToString();
        }

        #endregion

        #region Car counter
        private void CarCounterAdd()
        {
            carCounter = new CarCounter()
            {
                ContoursDraw = cbCCDraw.Checked,
                TrackingLineDraw = cbCCDraw.Checked,
                CounterDraw = cbCCDraw.Checked
            };

            carCounter.Init();
            carCounter.OnCarsDetected += this.OnCarsDetected;
        }

        private void OnCarsDetected(object sender, OnCVCarDetectedEventArgs e)
        {
            BeginInvoke(
                (Action)(() =>
                {
                    edCCDetectedCars.Text = e.CarsCount.ToString();
                }));
        }

        private void CarCounterRemove()
        {
            if (carCounter != null)
            {
                carCounter.OnCarsDetected -= this.OnCarsDetected;
                carCounter.Dispose();
                carCounter = null;
            }
        }

        #endregion

        //IntPtr pd = IntPtr.Zero;

        private void ProcessFrame(VideoFrame frame)
        {
            //if (pd == IntPtr.Zero)
            //{
            //    pd = CV.PedestrianDetectorInit();
            //    CV.PedestrianDetectorSetEngineSettings(pd, 0.5, 5, true, Color.YellowGreen);
            //}

            //long time;
            //CVPedestrians items = new CVPedestrians();
            //int count = CV.PedestrianDetectorProcess(pd, frame, ref items, out time);
            //Trace.WriteLine($"Count: {count}, time: {time}");

            var image = frame.ToRAWImage();
            var faces = faceDetector?.Process(image);
            carCounter?.Process(image);
            pedestrianDetector?.Process(image);

            if (cbFDMosaic.Checked)
            {
                if (faces != null)
                {
                    foreach (var face in faces)
                    {
                        var rect = face.Position;
                        rect.Top -= 10;
                        if (rect.Top < 0)
                        {
                            rect.Top = 0;
                        }

                        rect.Left -= 10;
                        if (rect.Left < 0)
                        {
                            rect.Left = 0;
                        }

                        rect.Bottom += 10;
                        if (rect.Bottom > image.Height)
                        {
                            rect.Bottom = image.Height;
                        }

                        rect.Right += 10;
                        if (rect.Right > image.Width)
                        {
                            rect.Right = image.Width;
                        }

                        MFP.EffectMosaicROI(frame.Data, image.Width, image.Height, 45, rect);
                    }
                }
            }
        }

        #region Video capture source

        private void VideoCapture1_OnVideoFrameBuffer(object sender, VideoFrameBufferEventArgs e)
        {
            ProcessFrame(e.Frame);
        }

        private void cbVideoInputDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbVideoInputDevice.SelectedIndex != -1)
            {
                cbVideoInputFormat.Items.Clear();

                var deviceItem = VideoCapture1.Video_CaptureDevices.FirstOrDefault(device => device.Name == cbVideoInputDevice.Text);
                if (deviceItem == null)
                {
                    return;
                }

                foreach (var format in deviceItem.VideoFormats)
                {
                    cbVideoInputFormat.Items.Add(format.Name);
                }

                if (cbVideoInputFormat.Items.Count > 0)
                {
                    cbVideoInputFormat.SelectedIndex = 0;
                }
            }
        }

        private void SelectIPCameraSource(out IPCameraSourceSettings settings)
        {
            settings = new IPCameraSourceSettings
            {
                URL = edIPUrl.Text
            };

            switch (cbIPCameraType.SelectedIndex)
            {
                case 0:
                    settings.Type = IPSourceEngine.Auto_VLC;
                    break;
                case 1:
                    settings.Type = IPSourceEngine.Auto_FFMPEG;
                    break;
                case 2:
                    settings.Type = IPSourceEngine.Auto_LAV;
                    break;
                case 3:
                    settings.Type = IPSourceEngine.MMS_WMV;
                    break;
            }

            settings.AudioCapture = false;
            settings.Login = edIPLogin.Text;
            settings.Password = edIPPassword.Text;
            settings.Debug_Enabled = cbDebugMode.Checked;
            settings.Debug_Filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VisioForge", "ip_cam_log.txt");
        }

        private void SelectVideoCaptureSource()
        {
            VideoCapture1.Video_CaptureDevice = new VideoCaptureSource(cbVideoInputDevice.Text);
            VideoCapture1.Video_CaptureDevice.Format_UseBest = cbUseBestVideoInputFormat.Checked;
            VideoCapture1.Video_CaptureDevice.Format = cbVideoInputFormat.Text;

            if (cbVideoInputFrameRate.SelectedIndex != -1)
            {
                VideoCapture1.Video_CaptureDevice.FrameRate = Convert.ToDouble(cbVideoInputFrameRate.Text, CultureInfo.CurrentCulture);
            }
        }

        private void Log(string txt)
        {
            if (IsHandleCreated)
            {
                Invoke((Action)(() => { mmLog.Text = mmLog.Text + txt + Environment.NewLine; }));
            }
        }

        private void VideoCapture1_OnError(object sender, ErrorsEventArgs e)
        {
            Log(e.Message);
        }
        
        private void ConfigureVideoCapture()
        {
            // select source
            VideoCapture1.Debug_Mode = cbDebugMode.Checked;
            VideoCapture1.Debug_Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VisioForge");
            VideoCapture1.VLC_Path = Environment.GetEnvironmentVariable("VFVLCPATH");

            if (rbVideoCaptureDevice.Checked)
            {
                VideoCapture1.Mode = VideoCaptureMode.VideoPreview;
            }
            else
            {
                VideoCapture1.Mode = VideoCaptureMode.IPPreview;
            }

            if ((VideoCapture1.Mode == VideoCaptureMode.IPCapture) || (VideoCapture1.Mode == VideoCaptureMode.IPPreview))
            {
                // from IP camera
                IPCameraSourceSettings settings;
                SelectIPCameraSource(out settings);
                VideoCapture1.IP_Camera_Source = settings;
            }
            else if ((VideoCapture1.Mode == VideoCaptureMode.VideoCapture) || (VideoCapture1.Mode == VideoCaptureMode.VideoPreview) ||
                (VideoCapture1.Mode == VideoCaptureMode.AudioCapture) || (VideoCapture1.Mode == VideoCaptureMode.AudioPreview))
            {
                // from video capture device
                SelectVideoCaptureSource();
            }

            VideoCapture1.Audio_RecordAudio = false;
            VideoCapture1.Audio_PlayAudio = false;

            VideoCapture1.Video_Sample_Grabber_Enabled = true;

            VideoCapture1.Video_Renderer_SetAuto();
        }

        #endregion

        #region Media Player

        private void ConfigureMediaPlayer()
        {
            MediaPlayer1.Debug_Mode = cbDebugMode.Checked;
            MediaPlayer1.Debug_Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VisioForge");

            MediaPlayer1.Source_Mode = MediaPlayerSourceMode.LAV;
            MediaPlayer1.FilenamesOrURL.Clear();
            MediaPlayer1.FilenamesOrURL.Add(edFilename.Text);

            MediaPlayer1.Video_Renderer_SetAuto();
        }

        private void MediaPlayer1_OnVideoFrameBuffer(object sender, VideoFrameBufferEventArgs e)
        {
            ProcessFrame(e.Frame);
        }

        #endregion

        private async void btStart_Click(object sender, EventArgs e)
        {
            mmLog.Clear();
            tcMain.SelectedIndex = 4;

            if (rbVideoFile.Checked)
            {
                ConfigureMediaPlayer();
            }
            else
            {
                ConfigureVideoCapture();
            }

            // add face detection
            if (cbFDEnabled.Checked)
            {
                FaceDetectionAdd();
            }

            // add car counter
            if (cbCCEnabled.Checked)
            {
                CarCounterAdd();
            }

            // add car counter
            if (cbPDEnabled.Checked)
            {
                PedestrianDetectionAdd();
            }

            //this.MediaPlayer1.Video_Effects_Enabled = true;
            //    this.MediaPlayer1.Video_Effects_Clear();
            //    this.MediaPlayer1.Video_Effects_Add(new VideoEffectMosaic(true, 500));

            if (rbVideoFile.Checked)
            {
                MediaPlayerView.Show();
                VideoCaptureView.Hide();
                await MediaPlayer1.PlayAsync();
            }
            else
            {
                MediaPlayerView.Hide();
                VideoCaptureView.Show();
                await VideoCapture1.StartAsync();
            }
        }

        private async void btStop_Click(object sender, EventArgs e)
        {
            await VideoCapture1.StopAsync();
            await MediaPlayer1.StopAsync();

            FaceDetectionRemove();
            CarCounterRemove();
            PedestrianDetectionRemove();
        }

        private void btOpenFile_Click(object sender, EventArgs e)
        {
            if (dlgOpenFile.ShowDialog() == DialogResult.OK)
            {
                edFilename.Text = dlgOpenFile.FileName;
            }
        }

        private void cbVideoInputFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cbVideoInputFormat.Text))
            {
                return;
            }

            if (cbVideoInputDevice.SelectedIndex != -1)
            {
                var deviceItem = VideoCapture1.Video_CaptureDevices.FirstOrDefault(device => device.Name == cbVideoInputDevice.Text);
                if (deviceItem == null)
                {
                    return;
                }

                var videoFormat = deviceItem.VideoFormats.FirstOrDefault(format => format.Name == cbVideoInputFormat.Text);
                if (videoFormat == null)
                {
                    return;
                }

                cbVideoInputFrameRate.Items.Clear();
                foreach (var frameRate in videoFormat.FrameRates)
                {
                    cbVideoInputFrameRate.Items.Add(frameRate.ToString(CultureInfo.CurrentCulture));
                }

                if (cbVideoInputFrameRate.Items.Count > 0)
                {
                    cbVideoInputFrameRate.SelectedIndex = 0;
                }
            }
        }
    }
}
