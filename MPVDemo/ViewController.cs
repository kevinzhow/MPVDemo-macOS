using System;
using System.Runtime.InteropServices;
using AppKit;
using Foundation;
using ObjCRuntime;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace MPVDemo
{
    public partial class ViewController : NSViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        private const int MpvFormatString = 1;
        private IntPtr _mpvHandle;
        private string _mediaFilePath;

        [DllImport("libmpv.1.25.0.dylib", EntryPoint = "mpv_create")]
        private static extern IntPtr MpvCreate();

        [DllImport("libmpv.1.25.0.dylib", EntryPoint = "mpv_initialize")]
        private static extern int MpvInitialize(IntPtr mpvHandle);

        [DllImport("libmpv.1.25.0.dylib", EntryPoint = "mpv_command")]
        private static extern int MpvCommand(IntPtr mpvHandle, IntPtr strings);


        [DllImport("libmpv.1.25.0.dylib", EntryPoint = "mpv_terminate_destroy")]
        private static extern int MpvTerminateDestroy(IntPtr mpvHandle);


        [DllImport("libmpv.1.25.0.dylib", EntryPoint = "mpv_set_option")]
        private static extern int MpvSetOption(IntPtr mpvHandle, byte[] name, int format, ref long data);


        [DllImport("libmpv.1.25.0.dylib", EntryPoint = "mpv_set_option_string")]
        private static extern int MpvSetOptionString(IntPtr mpvHandle, byte[] name, byte[] value);


        [DllImport("libmpv.1.25.0.dylib", EntryPoint = "mpv_get_property")]
        private static extern int MpvGetPropertystring(IntPtr mpvHandle, byte[] name, int format, ref IntPtr data);


        [DllImport("libmpv.1.25.0.dylib", EntryPoint = "mpv_set_property")]
        private static extern int MpvSetProperty(IntPtr mpvHandle, byte[] name, int format, ref byte[] data);


        [DllImport("libmpv.1.25.0.dylib", EntryPoint = "mpv_free")]
        private static extern int MpvFree(IntPtr data);



        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            _mpvHandle = MpvCreate();
        }

        public void Pause()
        {
            if (_mpvHandle == IntPtr.Zero)
                return;

            Debug.WriteLine("Time Position is {0} ", TimePosition, null);

            if (IsPaused()) {
                Play();
            } else {
                var bytes = GetUtf8Bytes("yes");
                MpvSetProperty(_mpvHandle, GetUtf8Bytes("pause"), MpvFormatString, ref bytes);
            }
        }

        public int TimePosition {
            get {
                
                var lpBuffer = IntPtr.Zero;
                MpvGetPropertystring(_mpvHandle, GetUtf8Bytes("time-pos"), 4, ref lpBuffer);
                var time = lpBuffer.ToInt32();
                Debug.WriteLine("Time Position is {0} duration is {1}", time, Duration, null);

                return time;
            }

        }

        public int Duration {
            get {
                var lpBuffer = IntPtr.Zero;
                MpvGetPropertystring(_mpvHandle, GetUtf8Bytes("duration"), 4, ref lpBuffer);

                int duration = lpBuffer.ToInt32();

                return duration;
            }
        }

        private void Play()
        {
            if (_mpvHandle == IntPtr.Zero)
                return;

            var bytes = GetUtf8Bytes("no");
            MpvSetProperty(_mpvHandle, GetUtf8Bytes("pause"), MpvFormatString, ref bytes);

        }

        public bool IsPaused()
        {
            if (_mpvHandle == IntPtr.Zero)
                return true;

            var lpBuffer = IntPtr.Zero;
            MpvGetPropertystring(_mpvHandle, GetUtf8Bytes("pause"), MpvFormatString, ref lpBuffer);
            var isPaused = Marshal.PtrToStringAnsi(lpBuffer) == "yes";
            MpvFree(lpBuffer);
            return isPaused;
        }

        public void SetTime(double value)
        {
            if (_mpvHandle == IntPtr.Zero)
                return;

            DoMpvCommand("seek", value.ToString(CultureInfo.InvariantCulture), "absolute");
        }

        private static byte[] GetUtf8Bytes(string s)
        {
            return Encoding.UTF8.GetBytes(s + "\0");
        }

        public static IntPtr AllocateUtf8IntPtrArrayWithSentinel(string[] arr, out IntPtr[] byteArrayPointers)
        {
            int numberOfStrings = arr.Length + 1; // add extra element for extra null pointer last (sentinel)
            byteArrayPointers = new IntPtr[numberOfStrings];
            IntPtr rootPointer = Marshal.AllocCoTaskMem(IntPtr.Size * numberOfStrings);
            for (int index = 0; index < arr.Length; index++)
            {
                var bytes = GetUtf8Bytes(arr[index]);
                IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
                byteArrayPointers[index] = unmanagedPointer;
            }
            Marshal.Copy(byteArrayPointers, 0, rootPointer, numberOfStrings);
            return rootPointer;
        }

        private void DoMpvCommand(params string[] args)
        {
            IntPtr[] byteArrayPointers;
            var mainPtr = AllocateUtf8IntPtrArrayWithSentinel(args, out byteArrayPointers);
            MpvCommand(_mpvHandle, mainPtr);
            foreach (var ptr in byteArrayPointers)
            {
                Marshal.FreeHGlobal(ptr);
            }
            Marshal.FreeHGlobal(mainPtr);
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
            if (_mpvHandle != IntPtr.Zero)
                MpvTerminateDestroy(_mpvHandle);

            _mpvHandle = MpvCreate();

            if (_mpvHandle == IntPtr.Zero) {
                Debug.WriteLine("Create MPV Failed");
            } else {
                Debug.WriteLine("Create MPV {0}", _mediaFilePath, null);
            }

            int mpvFormatInt64 = 4;
            var windowId = VideoView.Handle.ToInt64();
        
            MpvSetOption(_mpvHandle, GetUtf8Bytes("wid"), mpvFormatInt64, ref windowId);
          
            MpvSetOptionString(_mpvHandle, GetUtf8Bytes("log-file"), GetUtf8Bytes("/Users/zhoukaiwen/mpv.log"));

            MpvSetOptionString(_mpvHandle, GetUtf8Bytes("input-media-keys"), GetUtf8Bytes("yes"));
            MpvSetOptionString(_mpvHandle, GetUtf8Bytes("input-default-bindings"), GetUtf8Bytes("yes"));
            MpvSetOptionString(_mpvHandle, GetUtf8Bytes("input-cursor"), GetUtf8Bytes("no"));
            MpvSetOptionString(_mpvHandle, GetUtf8Bytes("input-vo-keyboard"), GetUtf8Bytes("yes"));

            MpvSetOptionString(_mpvHandle, GetUtf8Bytes("keep-open"), GetUtf8Bytes("always"));
            //_mpvRequestLogMessages(_mpvHandle, GetUtf8Bytes("warn"));


            var intmpv = MpvInitialize(_mpvHandle);
            Debug.WriteLine("Begin Play and init status {0}", intmpv, null);
            DoMpvCommand("loadfile", _mediaFilePath);
            Play();
        }


        partial void Pause(NSObject sender)
        {
            Pause();
        }
    }
}
