using System.Diagnostics;
using System.Globalization;
using ObjCRuntime;
using VisioForge.Core;
using VisioForge.Core.MediaBlocks;
using VisioForge.Core.MediaBlocks.Sinks;
using VisioForge.Core.MediaBlocks.Sources;
using VisioForge.Core.MediaBlocks.Special;
using VisioForge.Core.MediaBlocks.VideoEncoders;
using VisioForge.Core.MediaBlocks.VideoRendering;
using VisioForge.Core.Types.X.Sinks;
using VisioForge.Core.UI.Apple;

namespace ScreenCaptureMB;

public partial class ViewController : NSViewController
{
    private MediaBlocksPipeline _pipeline;

    private ScreenSourceBlock _source;

    private VideoRendererBlock _videoRenderer;

    private VideoViewGL _videoView;

    private TeeBlock _videoTee;

    private MP4SinkBlock _mp4Sink;

    private H264EncoderBlock _h264Encoder;

    /// <summary>
    /// The position timer.
    /// </summary>
    private System.Timers.Timer _tmPosition = new System.Timers.Timer(500);

    protected ViewController(NativeHandle handle) : base(handle)
    {
        // This constructor is required if the view controller is loaded from a xib or a storyboard.
        // Do not put any initialization here, use ViewDidLoad instead.
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        // Do any additional setup after loading the view.
        InvokeOnMainThread(() =>
        {
            //View.Window.Delegate = new CustomWindowDelegate();
        });

        _tmPosition.Elapsed += tmPosition_Elapsed;

        ScreenSourceBlock.AskPermissions();
    }

    private string GenerateFilename()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"capture_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.mp4");
    }

    private async Task CreateEngineAsync()
    {
        if (_pipeline != null)
        {
            await _pipeline.StopAsync();
            await _pipeline.DisposeAsync();
        }

        _pipeline = new MediaBlocksPipeline(true);

        _videoView = new VideoViewGL(new CGRect(0, 0, pnVideoViewX.Bounds.Width, pnVideoViewX.Bounds.Height));
        pnVideoViewX.AddSubview(_videoView);

        _pipeline.OnError += _pipeline_OnError;

        _source = new ScreenSourceBlock();
        _videoTee = new TeeBlock(2);

        var filename = GenerateFilename();
        _mp4Sink = new MP4SinkBlock(new MP4SinkSettings(filename));
        _h264Encoder = new H264EncoderBlock();
        _videoRenderer = new VideoRendererBlock(_pipeline, _videoView);

        _pipeline.Connect(_source.Output, _videoTee.Input);

        _pipeline.Connect(_videoTee.Outputs[0], _videoRenderer.Input);
        _pipeline.Connect(_videoTee.Outputs[1], _h264Encoder.Input);

        _pipeline.Connect(_h264Encoder.Output, _mp4Sink.CreateNewInput(MediaBlockPadMediaType.Video));
    }

    public override NSObject RepresentedObject
    {
        get => base.RepresentedObject;
        set
        {
            base.RepresentedObject = value;

            // Update the view, if already loaded.
        }
    }

    private async void OnStop(object sender, EventArgs e)
    {
        if (_pipeline != null)
        {
            _pipeline.OnError -= _pipeline_OnError;
            await _pipeline.StopAsync();
        }
    }

    private void _pipeline_OnError(object sender, VisioForge.Core.Types.Events.ErrorsEventArgs e)
    {
        Debug.WriteLine(e.Message);
    }

    private async Task StopAllAsync()
    {
        if (_pipeline == null)
        {
            return;
        }

        _tmPosition.Stop();

        if (_pipeline != null)
        {
            await _pipeline.StopAsync();
        }
    }

    /// <summary>
    /// Handles the Elapsed event of the tmPosition control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.Timers.ElapsedEventArgs"/> instance containing the event data.</param>
    private async void tmPosition_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        if (_pipeline == null)
        {
            return;
        }

        try
        {
            InvokeOnMainThread(async () =>
            {
                if (_pipeline == null)
                {
                    return;
                }

                lbDurationX.StringValue = $"{(await _pipeline.Position_GetAsync()).ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)}";
            });
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
        }
    }

    partial void btStartClickX(Foundation.NSObject sender)
    {
        InvokeOnMainThread(async () => {
            await CreateEngineAsync();

            await _pipeline.StartAsync();

            _tmPosition.Start();
        });
    }

    partial void btStopClickX(Foundation.NSObject sender)
    {
        InvokeOnMainThread(async () => {
            await StopAllAsync();
        });
    }
}


// Custom Window delegate to close the SDK
public class CustomWindowDelegate : NSWindowDelegate
{
    public override bool WindowShouldClose(NSObject sender)
    {
        VisioForgeX.DestroySDK();

        // Return true to allow the window to close, false to cancel.
        return true;
    }
}