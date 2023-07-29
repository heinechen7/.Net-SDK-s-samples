﻿using System;
using System.Threading.Tasks;
using System.Windows;
using VisioForge.Core.MediaPlayer;
using VisioForge.Core.Types.Events;
using VisioForge.Core.Types;
using VisioForge.Core.Types.MediaPlayer;
using System.Windows.Threading;
using VisioForge.Core.Types.VideoEffects.NvidiaMaxine;
using VisioForge.Core.Types.VideoEffects;

namespace Nvidia_Maxine_Player
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private MediaPlayerCore MediaPlayer1;

        private int _effectID = 0;

        private string _modelsPath;

        private readonly DispatcherTimer _timer = new DispatcherTimer();

        private async Task CreateEngineAsync()
        {
            MediaPlayer1 = await MediaPlayerCore.CreateAsync(VideoView1);
            MediaPlayer1.OnError += MediaPlayer1_OnError;
            MediaPlayer1.OnStop += MediaPlayer1_OnStop;
        }

        private void DestroyEngine()
        {
            if (MediaPlayer1 != null)
            {
                MediaPlayer1.OnError -= MediaPlayer1_OnError;
                MediaPlayer1.OnStop -= MediaPlayer1_OnStop;
                
                MediaPlayer1.Dispose();
                MediaPlayer1 = null;
            }
        }

        private async void tbTimeline_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Convert.ToInt32(_timer.Tag) == 0)
            {
                await MediaPlayer1.Position_Set_TimeAsync(TimeSpan.FromSeconds(tbTimeline.Value));
            }
        }

        private async void btStart_Click(object sender, RoutedEventArgs e)
        {
            edLog.Clear();

            MediaPlayer1.Source_Mode = MediaPlayerSourceMode.LAV;

            MediaPlayer1.Playlist_Clear();
            MediaPlayer1.Playlist_Add(edFilename.Text);

            MediaPlayer1.Loop = cbLoop.IsChecked == true;
            MediaPlayer1.Audio_PlayAudio = true;
            MediaPlayer1.Info_UseLibMediaInfo = true;
            MediaPlayer1.Audio_OutputDevice = "Default DirectSound Device";

            MediaPlayer1.Debug_Mode = cbDebug.IsChecked == true;

            _effectID = cbVideoEffect.SelectedIndex;
            _modelsPath = edModels.Text;
            MediaPlayer1.Video_Effects_Enabled = true;

            // add effects
            MediaPlayer1.Video_Effects_Clear();
            MediaPlayer1.Video_Resize = null;

            switch (cbVideoEffect.SelectedIndex)
            {
                case 0:
                    break;
                case 1:
                    {
                        var videoEffect = new MaxineDenoiseVideoEffect(
                            edModels.Text,
                            strength: (float)(slDenoiseStrength.Value / 10.0f));
                        MediaPlayer1.Video_Effects_Add(videoEffect);
                    }

                    break;
                case 2:
                    {
                        var videoEffect = new MaxineArtifactReductionVideoEffect(
                            edModels.Text,
                            mode: (MaxineArtifactReductionEffectMode)cbArtifactReductionMode.SelectedIndex);
                        MediaPlayer1.Video_Effects_Add(videoEffect);
                    }

                    break;
                case 3:
                    {
                        MediaPlayer1.Video_Resize = new MaxineUpscaleSettings(
                            edModels.Text,
                            height: Convert.ToInt32(edUpscaleHeight.Text),
                            strength: (float)(slUpscaleStrength.Value / 10.0f));
                    }

                    break;
                case 4:
                    MediaPlayer1.Video_Resize = new MaxineSuperResSettings(
                            edModels.Text,
                            height: Convert.ToInt32(edSuperResolutionHeight.Text));

                    break;
                //case 5:
                //    _videoEffect = new AIGSVideoEffect(mode: (AIGSEffectMode)cbAIGSMode.SelectedIndex, backgroundImage: edAIGSBackground.Text);
                //    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            await MediaPlayer1.PlayAsync();

            _timer.Start();
        }

        private async void btPause_Click(object sender, RoutedEventArgs e)
        {
            await MediaPlayer1.PauseAsync();
        }

        private async void btResume_Click(object sender, RoutedEventArgs e)
        {
            await MediaPlayer1.ResumeAsync();
        }

        private async void btStop_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();

            await MediaPlayer1.StopAsync();

            tbTimeline.Value = 0;
        }

        private void btOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                edFilename.Text = dlg.FileName;
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await CreateEngineAsync();

            this.Title += $" (SDK v{MediaPlayer1.SDK_Version()})";
            MediaPlayer1.Debug_Dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VisioForge");

            cbVideoEffect.SelectedIndex = 0;
            cbVideoEffect_SelectionChanged(null, null);

            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick +=
                async delegate (object s, EventArgs a)
                {
                    await timer1_Tick();
                };
        }

        private async Task timer1_Tick()
        {
            _timer.Tag = 1;
            tbTimeline.Maximum = (int)(await MediaPlayer1.Duration_TimeAsync()).TotalSeconds;

            int value = (int)(await MediaPlayer1.Position_Get_TimeAsync()).TotalSeconds;
            if ((value > 0) && (value < tbTimeline.Maximum))
            {
                tbTimeline.Value = value;
            }

            lbTime.Content = MediaPlayer1.Helpful_SecondsToTimeFormatted((int)tbTimeline.Value) + "/" + MediaPlayer1.Helpful_SecondsToTimeFormatted((int)tbTimeline.Maximum);

            _timer.Tag = 0;
        }

        private void MediaPlayer1_OnError(object sender, ErrorsEventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                edLog.Text = edLog.Text + e.Message + Environment.NewLine;
            }));
        }

        private void MediaPlayer1_OnStop(object sender, StopEventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                tbTimeline.Value = 0;
            }));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            btStop_Click(null, null);

            DestroyEngine();
        }

        private void cbVideoEffect_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (gdDenoise == null)
            {
                return;
            }

            switch (cbVideoEffect.SelectedIndex)
            {
                case 0:
                    {
                        gdDenoise.Visibility = Visibility.Collapsed;
                        gdArtifactReduction.Visibility = Visibility.Collapsed;
                        gdSuperResolution.Visibility = Visibility.Collapsed;
                        gdUpscale.Visibility = Visibility.Collapsed;
                        // gdAIGS.Visibility = Visibility.Collapsed;
                    }
                    break;
                case 1:
                    {
                        gdDenoise.Visibility = Visibility.Visible;
                        gdArtifactReduction.Visibility = Visibility.Collapsed;
                        gdSuperResolution.Visibility = Visibility.Collapsed;
                        gdUpscale.Visibility = Visibility.Collapsed;
                        // gdAIGS.Visibility = Visibility.Collapsed;
                    }
                    break;
                case 2:
                    {
                        gdDenoise.Visibility = Visibility.Collapsed;
                        gdArtifactReduction.Visibility = Visibility.Visible;
                        gdSuperResolution.Visibility = Visibility.Collapsed;
                        gdUpscale.Visibility = Visibility.Collapsed;
                        // gdAIGS.Visibility = Visibility.Collapsed;
                    }
                    break;
                case 3:
                    {
                        gdDenoise.Visibility = Visibility.Collapsed;
                        gdArtifactReduction.Visibility = Visibility.Collapsed;
                        gdSuperResolution.Visibility = Visibility.Visible;
                        gdUpscale.Visibility = Visibility.Collapsed;
                        // gdAIGS.Visibility = Visibility.Collapsed;
                    }
                    break;
                case 4:
                    {
                        gdDenoise.Visibility = Visibility.Collapsed;
                        gdArtifactReduction.Visibility = Visibility.Collapsed;
                        gdSuperResolution.Visibility = Visibility.Collapsed;
                        gdUpscale.Visibility = Visibility.Visible;
                        // gdAIGS.Visibility = Visibility.Collapsed;
                    }
                    break;
                //case 5:
                //    {
                //        gdDenoise.Visibility = Visibility.Collapsed;
                //        gdArtifactReduction.Visibility = Visibility.Collapsed;
                //        gdSuperResolution.Visibility = Visibility.Collapsed;
                //        gdUpscale.Visibility = Visibility.Collapsed;
                //        gdAIGS.Visibility = Visibility.Visible;
                //    }
                //    break;

                default:
                    break;
            }
        }
    }
}
