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

namespace MPVDemo
{
    public partial class ViewController : NSViewController
    {

        private mpv mpvPlayer;
        private string _mediaFilePath;

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            //var lib = Dlfcn.dlopen("/Users/zhoukaiwen/Projects/MPVDemo/MPVDemo/bin/Debug/MPVDemo.app/Contents/MonoBundle/libavformat.57.dylib", 0);
            //if (lib == IntPtr.Zero) {
            //    Debug.WriteLine("Faild load");
            //} else {
            //    Debug.WriteLine("Finished load");
            //}
            ffmpeg.RootPath = NSBundle.MainBundle.BundlePath + "/Contents/MonoBundle/";
            ffmpeg.av_register_all();
            ffmpeg.avcodec_register_all();
            ffmpeg.avformat_network_init();

            base.ViewDidLoad();

            mpvPlayer = new mpv(VideoView.Handle);
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
                    Debug.WriteLine("We have url: {0}", path, null);

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
