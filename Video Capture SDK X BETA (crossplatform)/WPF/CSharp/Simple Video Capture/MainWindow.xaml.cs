﻿using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VisioForge.Core;
using VisioForge.Core.Helpers;
using VisioForge.Core.Types;
using VisioForge.Core.Types.Events;
using VisioForge.Core.Types.VideoEffects;
using VisioForge.Core.Types.X.Output;
using VisioForge.Core.Types.X.Sources;
using VisioForge.Core.Types.X.VideoEffects;
using VisioForge.Core.Types.X.VideoEncoders;
using VisioForge.Core.UI;
using VisioForge.Core.UI.WPF.Dialogs.OutputFormats;
using VisioForge.Core.VideoCaptureX;

namespace Simple_Video_Capture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IDisposable
    {
        private const AudioCaptureDeviceAPI AUDIO_API = AudioCaptureDeviceAPI.DirectSound;

        private UniversalOutputDialog mpegTSSettingsDialog;

        private UniversalOutputDialog movSettingsDialog;

        private UniversalOutputDialog mp4SettingsDialog;

        private UniversalOutputDialog aviSettingsDialog;

        private UniversalOutputDialog webMSettingsDialog;

        private readonly Microsoft.Win32.SaveFileDialog screenshotSaveDialog = new Microsoft.Win32.SaveFileDialog()
        {
            FileName = "image.jpg",
            Filter = "JPEG|*.jpg|BMP|*.bmp|PNG|*.png|GIF|*.gif|TIFF|*.tiff",
            InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VisioForge")
        };

        private System.Timers.Timer tmRecording = new System.Timers.Timer(1000);

        private VideoCaptureCoreX VideoCapture1;

        private bool disposedValue;

        public MainWindow()
        {
            InitializeComponent();

            System.Windows.Forms.Application.EnableVisualStyles();
        }

        private void CreateEngine()
        {
            VideoCapture1 = new VideoCaptureCoreX(VideoView1 as IVideoView);

            VideoCapture1.OnError += VideoCapture1_OnError;
        }

        private void DestroyEngine()
        {
            if (VideoCapture1 != null)
            {
                VideoCapture1.OnError -= VideoCapture1_OnError;

                VideoCapture1.Dispose();
                VideoCapture1 = null;
            }
        }

        private static bool SaveFileDialog(string defaultExt, string filter, out string filename)
        {
            filename = string.Empty;

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                DefaultExt = defaultExt,
                Filter = filter
            };

            if (dlg.ShowDialog() == true)
            {
                filename = dlg.FileName;
                return true;
            }

            return false;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CreateEngine();

            Title += $" (SDK v{VideoCapture1.SDK_Version})";

            tmRecording.Elapsed += (senderx, args) => { UpdateRecordingTime(); };
            
            // video input
            foreach (var device in await DeviceEnumerator.VideoSourcesAsync())
            {
                cbVideoInputDevice.Items.Add(device.Name);
            }

            if (cbVideoInputDevice.Items.Count > 0)
            {
                cbVideoInputDevice.SelectedIndex = 0;
            }

            cbVideoInputDevice_SelectionChanged(null, null);

            // audio input
            foreach (var device in (await DeviceEnumerator.AudioSourcesAsync()).Where(device => device.API == AUDIO_API))
            {
                cbAudioInputDevice.Items.Add(device.Name);
            }

            if (cbAudioInputDevice.Items.Count > 0)
            {
                cbAudioInputDevice.SelectedIndex = 0;
                cbAudioInputDevice_SelectionChanged(null, null);
            }

            // audio output
            string defaultAudioRenderer = string.Empty;
            foreach (var audioOutputDevice in (await DeviceEnumerator.AudioOutputsAsync()).Where(device => device.API == AudioOutputDeviceAPI.DirectSound))
            {
                cbAudioOutputDevice.Items.Add(audioOutputDevice.Name);

                if (audioOutputDevice.Name.Contains("Default"))
                {
                    defaultAudioRenderer = audioOutputDevice.Name;
                }
            }

            if (cbAudioOutputDevice.Items.Count > 0)
            {
                if (string.IsNullOrEmpty(defaultAudioRenderer))
                {
                    cbAudioOutputDevice.SelectedIndex = 0;
                }
                else
                {
                    cbAudioOutputDevice.Text = defaultAudioRenderer;
                }
            }

            edOutput.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VisioForge", "output.mp4");
        }

        private void btSelectOutput_Click(object sender, RoutedEventArgs e)
        {
            string filename;
            if (SaveFileDialog("*.mp4; *.webm; *.ts; *.avi; *.mov", "Video files|*.mp4; *.webm; *.ts; *.avi; *.mov", out filename))
            {
                edOutput.Text = filename;
            }
        }

        private void Log(string txt)
        {
            Dispatcher.Invoke(() => { mmLog.Text = mmLog.Text + txt + Environment.NewLine; });
        }

        private void VideoCapture1_OnError(object sender, ErrorsEventArgs e)
        {
            Log(e.Message);
        }

        private void tbAudioVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VideoCapture1 == null)
            {
                return;
            }

            VideoCapture1.Audio_OutputDevice_Volume = tbAudioVolume.Value / 100.0f;
        }

        private void lbViewVideoTutorials_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var startInfo = new ProcessStartInfo("explorer.exe", HelpLinks.VideoTutorials);
            Process.Start(startInfo);
        }

        private async void cbVideoInputDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbVideoInputDevice.SelectedIndex != -1 && e != null && e.AddedItems.Count > 0)
            {
                cbVideoInputFormat.Items.Clear();

                var deviceItem =
                    (await DeviceEnumerator.VideoSourcesAsync()).FirstOrDefault(device => device.Name == e.AddedItems[0].ToString());
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
                    cbVideoInputFormat_SelectionChanged(null, null);
                }
            }
        }

        private async void cbAudioInputDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbAudioInputDevice.SelectedIndex != -1 && e != null && e.AddedItems.Count > 0)
            {
                cbAudioInputFormat.Items.Clear();

                var deviceItem = (await DeviceEnumerator.AudioSourcesAsync()).FirstOrDefault(device => device.Name == e.AddedItems[0].ToString() && device.API == AUDIO_API);
                if (deviceItem == null)
                {
                    return;
                }

                var defaultValue = "S16LE 44100 Hz 2 ch.";
                var defaultValueExists = false;
                foreach (var format in deviceItem.Formats)
                {
                    cbAudioInputFormat.Items.Add(format.Name);

                    if (defaultValue == format.Name)
                    {
                        defaultValueExists = true;
                    }
                }

                if (cbAudioInputFormat.Items.Count > 0)
                {
                    cbAudioInputFormat.SelectedIndex = 0;

                    if (defaultValueExists)
                    {
                        cbAudioInputFormat.Text = defaultValue;
                    }
                }
            }
        }
        
        private async void btStart_Click(object sender, RoutedEventArgs e)
        {
            mmLog.Clear();

            VideoCapture1.Debug_Mode = cbDebugMode.IsChecked == true;
            VideoCapture1.Debug_Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VisioForge");

            VideoCapture1.Audio_OutputDevice = (await DeviceEnumerator.AudioOutputsAsync()).Where(device => device.Name == cbAudioOutputDevice.Text && device.API == AudioOutputDeviceAPI.DirectSound).First(); 

            if (cbRecordAudio.IsChecked == true)
            {
                VideoCapture1.Audio_Record = true;
                VideoCapture1.Audio_Play = true;
            }
            else
            {
                VideoCapture1.Audio_Record = false;
                VideoCapture1.Audio_Play = false;
            }

            // video source
            VideoCaptureDeviceSourceSettings videoSourceSettings = null;

            var deviceName = cbVideoInputDevice.Text;
            var format = cbVideoInputFormat.Text;
            if (!string.IsNullOrEmpty(deviceName) && !string.IsNullOrEmpty(format))
            {
                var device = (await DeviceEnumerator.VideoSourcesAsync()).FirstOrDefault(x => x.Name == deviceName);
                if (device != null)
                {
                    var formatItem = device.VideoFormats.FirstOrDefault(x => x.Name == format);
                    if (formatItem != null)
                    {
                        videoSourceSettings = new VideoCaptureDeviceSourceSettings(device)
                        {
                            Format = formatItem.ToFormat()
                        };

                        videoSourceSettings.Format.FrameRate = new VideoFrameRate(Convert.ToDouble(cbVideoInputFrameRate.Text));
                    }
                }
            }

            VideoCapture1.Video_Source = videoSourceSettings;

            // audio source
            DSAudioCaptureDeviceSourceSettings audioSourceSettings = null;

            deviceName = cbAudioInputDevice.Text;
            format = cbAudioInputFormat.Text;
            if (!string.IsNullOrEmpty(deviceName))
            {
                var device = (await DeviceEnumerator.AudioSourcesAsync()).FirstOrDefault(x => x.Name == deviceName && x.API == AUDIO_API);
                if (device != null)
                {
                    var formatItem = device.Formats.FirstOrDefault(x => x.Name == format);
                    if (formatItem != null)
                    {
                        audioSourceSettings = new DSAudioCaptureDeviceSourceSettings(device, formatItem.ToFormat());
                    }
                }
            }

            VideoCapture1.Audio_Source = audioSourceSettings;

            if (rbPreview.IsChecked == true)
            {
            }
            else
            {
                VideoCapture1.Outputs_Clear();
                
                switch (cbOutputFormat.SelectedIndex)
                {
                    case 0:
                        {
                            if (mp4SettingsDialog == null)
                            {
                                VideoCapture1.Outputs_Add(new MP4Output(edOutput.Text), false);
                            }
                            else
                            {
                                VideoCapture1.Outputs_Add(mp4SettingsDialog.GetOutputVC(), false);
                            }
                            
                            break;
                        }
                    case 1:
                        {
                            if (aviSettingsDialog == null)
                            {
                                VideoCapture1.Outputs_Add(new AVIOutput(edOutput.Text), false);
                            }
                            else
                            {
                                VideoCapture1.Outputs_Add(aviSettingsDialog.GetOutputVC(), false);
                            }

                            break;
                        }
                    case 2:
                        {
                            if (webMSettingsDialog == null)
                            {
                                VideoCapture1.Outputs_Add(new WebMOutput(edOutput.Text), false);
                            }
                            else
                            {
                                VideoCapture1.Outputs_Add(webMSettingsDialog.GetOutputVC(), false);
                            }
                            
                            break;
                        }
                    case 3:
                        {
                            if (mpegTSSettingsDialog == null)
                            {
                                VideoCapture1.Outputs_Add(new MPEGTSOutput(edOutput.Text), false);
                            }
                            else
                            {
                                VideoCapture1.Outputs_Add(mpegTSSettingsDialog.GetOutputVC(), false);
                            }
                            

                            break;
                        }
                    case 4:
                        {
                            if (movSettingsDialog == null)
                            {
                                VideoCapture1.Outputs_Add(new MOVOutput(edOutput.Text), false);
                            }
                            else
                            {
                                VideoCapture1.Outputs_Add(movSettingsDialog.GetOutputVC(), false);
                            }

                            break;
                        }
                }
            }

            ConfigureVideoEffects();

            await VideoCapture1.StartAsync();

            tcMain.SelectedIndex = 3;
            tmRecording.Start();

            //VideoCapture1.Debug_SavePipeline("videocapturex");
        }

        private void ConfigureVideoEffects()
        {
            VideoCapture1.Video_Effects_Clear();

            if (cbVideoBalance.IsChecked == true)
            {
                var balance = new VideoBalanceVideoEffect();
                VideoCapture1.Video_Effects_AddOrUpdate(balance);

                tbBrightness_Scroll(null, null);
                tbSaturation_Scroll(null, null);
                tbContrast_Scroll(null, null);
                tbHue_Scroll(null, null);
            }
                            
            if (cbGreyscale.IsChecked == true)
            {
                cbGreyscale_CheckedChanged(null, null);
            }

            if (cbSepia.IsChecked == true)
            {
                cbSepia_CheckedChanged(null, null);
            }

            if (cbFlipX.IsChecked == true)
            {
                cbFlipX_Checked(null, null);
            }

            if (cbFlipY.IsChecked == true)
            {
                cbFlipY_Checked(null, null);
            }

            if (cbTextLogo.IsChecked == true)
            {
                cbTextLogo_Click(null, null);
            }

            if (cbImageLogo.IsChecked == true)
            {
                cbImageLogo_Click(null, null);
            }
        }

        private async void btResume_Click(object sender, RoutedEventArgs e)
        {
            await VideoCapture1.ResumeAsync();
        }

        private async void btPause_Click(object sender, RoutedEventArgs e)
        {
            await VideoCapture1.PauseAsync();
        }

        private async void btStop_Click(object sender, RoutedEventArgs e)
        {
            tmRecording.Stop();

            await VideoCapture1.StopAsync();
        }

        private async void cbVideoInputFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cbVideoInputFrameRate.Items.Clear();

            if (string.IsNullOrEmpty(cbVideoInputFormat.SelectedValue?.ToString()) || string.IsNullOrEmpty(cbVideoInputDevice.SelectedValue.ToString()))
            {
                return;
            }

            if (cbVideoInputDevice.SelectedIndex != -1)
            {
                var deviceItem = (await DeviceEnumerator.VideoSourcesAsync()).FirstOrDefault(device => device.Name == cbVideoInputDevice.SelectedValue.ToString());
                if (deviceItem == null)
                {
                    return;
                }

                var videoFormat = deviceItem.VideoFormats.FirstOrDefault(format => format.Name == cbVideoInputFormat.SelectedValue.ToString());
                if (videoFormat == null)
                {
                    return;
                }

                // build int range from tuple (min, max)    
                var frameRateList = videoFormat.GetFrameRateRangeAsStringList();
                foreach (var item in frameRateList)
                {
                    cbVideoInputFrameRate.Items.Add(item);
                }

                if (cbVideoInputFrameRate.Items.Count > 0)
                {
                    cbVideoInputFrameRate.SelectedIndex = 0;
                }
            }
        }

        private async void btSaveScreenshot_Click(object sender, RoutedEventArgs e)
        {
            if (screenshotSaveDialog.ShowDialog() == true)
            {
                var filename = screenshotSaveDialog.FileName;
                var ext = Path.GetExtension(filename).ToLowerInvariant();
                switch (ext)
                {
                    case ".bmp":
                        await VideoCapture1.Snapshot_SaveAsync(filename, ImageFormat.Bmp);
                        break;
                    case ".jpg":
                        await VideoCapture1.Snapshot_SaveAsync(filename, ImageFormat.Jpeg, 85);
                        break;
                    case ".gif":
                        await VideoCapture1.Snapshot_SaveAsync(filename, ImageFormat.Gif);
                        break;
                    case ".png":
                        await VideoCapture1.Snapshot_SaveAsync(filename, ImageFormat.Png);
                        break;
                    case ".tiff":
                        await VideoCapture1.Snapshot_SaveAsync(filename, ImageFormat.Tiff);
                        break;
                }
            }
        }

        private void btOutputConfigure_Click(object sender, RoutedEventArgs e)
        {
            switch (cbOutputFormat.SelectedIndex)
            {
                case 0:
                    {
                        MP4Output mp4;
                        if (mp4SettingsDialog == null)
                        {
                            mp4 = new MP4Output(edOutput.Text);             
                        }
                        else
                        {
                            mp4 = (MP4Output)mp4SettingsDialog.GetOutput();
                        }

                        mp4SettingsDialog = new UniversalOutputDialog(mp4);
                        mp4SettingsDialog.ShowDialog();

                        break;
                    }
                case 1:
                    {
                        AVIOutput avi;
                        if (aviSettingsDialog == null)
                        {
                            avi = new AVIOutput(edOutput.Text);                            
                        }
                        else
                        {
                            avi = (AVIOutput)aviSettingsDialog.GetOutput();
                        }

                        aviSettingsDialog = new UniversalOutputDialog(avi);
                        aviSettingsDialog.ShowDialog();

                        break;
                    }
                case 2:
                    {
                        WebMOutput webm;
                        if (webMSettingsDialog == null)
                        {
                            webm = new WebMOutput(edOutput.Text);                            
                        }
                        else
                        {
                            webm = (WebMOutput)webMSettingsDialog.GetOutput();
                        }

                        webMSettingsDialog = new UniversalOutputDialog(webm);
                        webMSettingsDialog.ShowDialog();

                        break;
                    }
                case 3:
                    {
                        MPEGTSOutput ts;
                        if (mpegTSSettingsDialog == null)
                        {
                            ts = new MPEGTSOutput(edOutput.Text);                            
                        }
                        else
                        {
                            ts = (MPEGTSOutput)mpegTSSettingsDialog.GetOutput();
                        }

                        mpegTSSettingsDialog = new UniversalOutputDialog(ts);
                        mpegTSSettingsDialog.ShowDialog();
                        
                        break;
                    }
                case 4:
                    {
                        MOVOutput mov;
                        if (movSettingsDialog == null)
                        {
                            mov = new MOVOutput(edOutput.Text);
                        }
                        else
                        {
                            mov = (MOVOutput)movSettingsDialog.GetOutput();
                        }

                        movSettingsDialog = new UniversalOutputDialog(mov);
                        movSettingsDialog.ShowDialog();
                        
                        break;
                    }
            }
        }

        private void cbOutputFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (edOutput == null)
            {
                return;
            }

            switch (cbOutputFormat.SelectedIndex)
            {
                case 0:
                    {
                        edOutput.Text = FilenameHelper.ChangeFileExt(edOutput.Text, ".mp4");
                        break;
                    }
                case 1:
                    {
                        edOutput.Text = FilenameHelper.ChangeFileExt(edOutput.Text, ".avi");
                        break;
                    }
                case 2:
                    {
                        edOutput.Text = FilenameHelper.ChangeFileExt(edOutput.Text, ".webm"); 
                        break;
                    }
                case 3:
                    {
                        edOutput.Text = FilenameHelper.ChangeFileExt(edOutput.Text, ".ts");
                        break;
                    }
                case 4:
                    {
                        edOutput.Text = FilenameHelper.ChangeFileExt(edOutput.Text, ".mov");
                        break;
                    }
            }
        }

        private async void UpdateRecordingTime()
        {
            var ts = await VideoCapture1.DurationAsync();

            if (Math.Abs(ts.TotalMilliseconds) < 0.01)
            {
                return;
            }

            Dispatcher.Invoke((Action)(() =>
            {
                lbTimestamp.Text = "Duration: " + ts.ToString(@"hh\:mm\:ss");
            }));
        }

        private void cbGreyscale_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cbGreyscale.IsChecked == true)
            {
                var grayscale = new GrayscaleVideoEffect();
                VideoCapture1.Video_Effects_AddOrUpdate(grayscale);
            }
            else
            {
                VideoCapture1.Video_Effects_Remove(GrayscaleVideoEffect.DefaultName);
            }
        }

        private void tbContrast_Scroll(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VideoCapture1 == null)
            {
                return;
            }

            var balanceEffect = (VideoBalanceVideoEffect)VideoCapture1.Video_Effects_Get(VideoBalanceVideoEffect.DefaultName);
            if (balanceEffect != null)
            {
                balanceEffect.Contrast = tbContrast.Value / 100.0;
            }
        }

        private void tbHue_Scroll(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VideoCapture1 == null)
            {
                return;
            }

            var balanceEffect = (VideoBalanceVideoEffect)VideoCapture1.Video_Effects_Get(VideoBalanceVideoEffect.DefaultName);
            if (balanceEffect != null)
            {
                balanceEffect.Hue = tbHue.Value / 100.0;
            }
        }

        private void tbBrightness_Scroll(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VideoCapture1 == null)
            {
                return;
            }

            var balanceEffect = (VideoBalanceVideoEffect)VideoCapture1.Video_Effects_Get(VideoBalanceVideoEffect.DefaultName);
            if (balanceEffect != null)
            {
                balanceEffect.Brightness = tbBrightness.Value / 100.0;
            }            
        }

        private void tbSaturation_Scroll(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VideoCapture1 == null)
            {
                return;
            }

            var balanceEffect = (VideoBalanceVideoEffect)VideoCapture1.Video_Effects_Get(VideoBalanceVideoEffect.DefaultName);
            if (balanceEffect != null)
            {
                balanceEffect.Saturation = tbSaturation.Value / 100.0;
            }
        }

        private void cbSepia_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (VideoCapture1 == null)
            {
                return;
            }

            if (cbSepia.IsChecked == true)
            {
                var effect = new ColorEffectsVideoEffect(ColorEffectsPreset.Sepia);
                VideoCapture1.Video_Effects_AddOrUpdate(effect);
            }
            else
            {
                VideoCapture1.Video_Effects_Remove(ColorEffectsVideoEffect.DefaultName);
            }
        }

        private void cbFlipX_Checked(object sender, RoutedEventArgs e)
        {
            if (VideoCapture1 == null)
            {
                return;
            }

            if (cbFlipX.IsChecked == true)
            {
                var flip = new FlipRotateVideoEffect(VideoFlipRotateMethod.MethodHorizontal, "flipx");
                VideoCapture1.Video_Effects_AddOrUpdate(flip);
            }
            else
            {
                VideoCapture1.Video_Effects_Remove("flipx");
            }
        }

        private void cbFlipY_Checked(object sender, RoutedEventArgs e)
        {
            if (VideoCapture1 == null)
            {
                return;
            }

            if (cbFlipY.IsChecked == true)
            {
                var flip = new FlipRotateVideoEffect(VideoFlipRotateMethod.MethodVertical, "flipy");
                VideoCapture1.Video_Effects_AddOrUpdate(flip);
            }
            else
            {
                VideoCapture1.Video_Effects_Remove("flipy");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DestroyEngine();
        }

        private async void btStartCapture(object sender, RoutedEventArgs e)
        {
            await VideoCapture1.StartCaptureAsync(0, edOutput.Text);
        }

        private async void btStopCapture(object sender, RoutedEventArgs e)
        {
            await VideoCapture1.StopCaptureAsync(0);
        }

        private void cbTextLogo_Click(object sender, RoutedEventArgs e)
        {
            if (VideoCapture1 == null)
            {
                return;
            }

            if (cbTextLogo.IsChecked == true)
            {
                var effect = new TextOverlayVideoEffect();
                VideoCapture1.Video_Effects_AddOrUpdate(effect);
            }
            else
            {
                VideoCapture1.Video_Effects_Remove(TextOverlayVideoEffect.DefaultName);
            }
        }

        private void cbImageLogo_Click(object sender, RoutedEventArgs e)
        {
            if (VideoCapture1 == null)
            {
                return;
            }

            if (cbImageLogo.IsChecked == true)
            {
                var effect = new ImageOverlayVideoEffect("logo.png");
                VideoCapture1.Video_Effects_AddOrUpdate(effect);
            }
            else
            {
                VideoCapture1.Video_Effects_Remove(ImageOverlayVideoEffect.DefaultName);
            }
        }

        private void cbVideoBalance_Click(object sender, RoutedEventArgs e)
        {
            if (cbVideoBalance.IsChecked == true)
            {
                var effect = new VideoBalanceVideoEffect();
                VideoCapture1.Video_Effects_AddOrUpdate(effect);

                tbBrightness_Scroll(null, null);
                tbSaturation_Scroll(null, null);
                tbContrast_Scroll(null, null);
                tbHue_Scroll(null, null);
            }
            else
            {
                VideoCapture1.Video_Effects_Remove(VideoBalanceVideoEffect.DefaultName);
            }
        }

        #region Dispose

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    tmRecording?.Dispose();
                    tmRecording = null;

                    VideoCapture1?.Dispose();
                    VideoCapture1 = null;
                }

                disposedValue = true;
            }
        }

        ~MainWindow()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}