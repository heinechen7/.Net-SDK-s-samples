﻿using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

using VisioForge.Core.MediaBlocks;
using VisioForge.Core.MediaBlocks.AudioRendering;
using VisioForge.Core.MediaBlocks.Sources;
using VisioForge.Core.MediaBlocks.VideoRendering;
using VisioForge.Core.Types.Events;
using VisioForge.Core.Types.X.Sources;

namespace VNC_Source_Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MediaBlocksPipeline _pipeline;

        private VideoRendererBlock _videoRenderer;

        //private AudioRendererBlock _audioRenderer;

        private VNCSourceBlock _source;

        private System.Timers.Timer tmRecording = new System.Timers.Timer(1000);

        public MainWindow()
        {
            InitializeComponent();

            System.Windows.Forms.Application.EnableVisualStyles();

            _pipeline = new MediaBlocksPipeline(true);
            _pipeline.OnError += Pipeline_OnError;
        }

        private void Pipeline_OnError(object sender, ErrorsEventArgs e)
        {
            Debug.WriteLine(e.Message);
        }

        private void CreateEngine()
        {
            _pipeline = new MediaBlocksPipeline(true);
            _pipeline.OnError += Pipeline_OnError;
        }

        private void DestroyEngine()
        {
            if (_pipeline != null)
            {
                _pipeline.OnError -= Pipeline_OnError;

                _pipeline.Dispose();
                _pipeline = null;
            }
        }

        private void UpdateRecordingTime()
        {            
            //var ts = _pipeline.Duration();

            //if (Math.Abs(ts.TotalMilliseconds) < 0.01)
            //{
            //    return;
            //}

            //Dispatcher.BeginInvoke((Action)(() =>
            //{
            //    lbTimestamp.Text = "Recording time: " + ts.ToString(@"hh\:mm\:ss");
            //}));
        }

        private async void btStart_Click(object sender, RoutedEventArgs e)
        {
            CreateEngine();

            //_pipeline.Debug_Mode = cbDebugMode.IsChecked == true;
            _pipeline.Debug_Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VisioForge");

            var vncSettings = new VNCSourceSettings(); ;
            if (rbHost.IsChecked == true)
            {
                vncSettings.Host = edHost.Text;
                vncSettings.Port = Convert.ToInt32(edPort.Text);
            }
            else
            {
                vncSettings.Uri = edURL.Text;
            }

            
            vncSettings.Password = edPassword.Text;

            _source = new VNCSourceBlock(vncSettings);
            _videoRenderer = new VideoRendererBlock(_pipeline, VideoView1);
            //_audioRenderer = new AudioRendererBlock();

            _pipeline.Connect(_source.Output, _videoRenderer.Input);
           // _pipeline.Connect(_ndiSource.AudioOutput, _audioRenderer.Input);

            await _pipeline.StartAsync();

            //_pipeline.SavePipeline("ndi_source");

            tmRecording.Start();
        }

        private async void btStop_Click(object sender, RoutedEventArgs e)
        {
            tmRecording.Stop();

            await _pipeline.StopAsync();

            DestroyEngine();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CreateEngine();

            Title += $" (SDK v{MediaBlocksPipeline.SDK_Version})";

            tmRecording.Elapsed += (senderx, args) => { UpdateRecordingTime(); };
        }
    }
}
