using System;
using System.Runtime.InteropServices;
using AppKit;
using Foundation;
using ObjCRuntime;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using ExtraLib.FFmpeg;
using ExtraLib;
using System.Threading;
using CoreGraphics;
using Xabe.FFmpeg;
using System.IO;

namespace MPVDemo
{
    public partial class ViewController : NSViewController
    {

        private mpv mpvPlayer;
        private string _mediaFilePath;
        //private FFmpeg ffmpegConverter;

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {


            base.ViewDidLoad();

            UseFFmpegCommandLine();
                //
            //ffmpegConverter = new FFmpeg();
            mpvPlayer = new mpv(VideoView.Handle);
        }

        async void UseFFmpegCommandLine() {
            Xabe.FFmpeg.FFbase.FFmode = Xabe.FFmpeg.Enums.Mode.FFmpeg;
            Xabe.FFmpeg.FFbase.FFmpegDir = NSBundle.MainBundle.BundlePath + "/Contents/Frameworks";
           
            string output = Path.ChangeExtension("/Users/zhoukaiwen/Downloads/hello", ".mp4");
            bool result = await ConversionHelper.ToMp4("/Users/zhoukaiwen/Downloads/IMG_0875.MOV", output).Start();
            Console.WriteLine("Convert media {0}", result);
        }

        partial void ChooseFile(NSObject sender)
        {

            var dlg = NSOpenPanel.OpenPanel;
            dlg.CanChooseFiles = true;
            dlg.CanChooseDirectories = false;
            dlg.AllowedFileTypes = new string[] { "mp4", "mov", "mkv" };

            if (dlg.RunModal() == 1)
            {
                // Nab the first file
                var url = dlg.Urls[0];
                var filename = url.LastPathComponent;
                if (url != null)
                {
                    var path = url.Path;
                    _mediaFilePath = path;
                    mpvPlayer.LoadFile(path);
                    Debug.WriteLine("We have url: {0}", path, null);

                    Thread thread = new Thread(() => {
                        // Use Dylibs
                        //ffmpegConverter.ProcessWithFFmpeg(path);
                    });

                    thread.Start();
                }
            }
        }


        partial void Play(NSObject sender)
        {
            mpvPlayer.LoadFile(_mediaFilePath);

        }

        partial void Pause(NSObject sender)
        {
            mpvPlayer.Pause();
        }


        partial void MakeScreenShot(NSObject sender)
        {
            mpvPlayer.MakeScreenShot();
            Debug.WriteLine("ScreenShot time position is {0}", mpvPlayer.TimePosition, null);
        }
    }
}
