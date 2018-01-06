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
        private IntPtr _mpvDll;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr MpvCreate();
        private MpvCreate _mpvCreate;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvInitialize(IntPtr mpvHandle);
        private MpvInitialize _mpvInitialize;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvCommand(IntPtr mpvHandle, IntPtr strings);
        private MpvCommand _mpvCommand;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvTerminateDestroy(IntPtr mpvHandle);
        private MpvTerminateDestroy _mpvTerminateDestroy;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvSetOption(IntPtr mpvHandle, byte[] name, int format, ref long data);
        private MpvSetOption _mpvSetOption;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvSetOptionString(IntPtr mpvHandle, byte[] name, byte[] value);
        private MpvSetOptionString _mpvSetOptionString;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvGetPropertystring(IntPtr mpvHandle, byte[] name, int format, ref IntPtr data);
        private MpvGetPropertystring _mpvGetPropertyString;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvSetProperty(IntPtr mpvHandle, byte[] name, int format, ref byte[] data);
        private MpvSetProperty _mpvSetProperty;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void MpvFree(IntPtr data);
        private MpvFree _mpvFree;

        private object GetDllType(Type type, string name)
        {
            IntPtr address = Dlfcn.dlsym(_mpvDll, "mpv_create");
            if (address != IntPtr.Zero) {
                Debug.WriteLine("Find method");
                return Marshal.GetDelegateForFunctionPointer(address, type);
            } else {
                Debug.WriteLine("Find method None");
                return null;
            }

        }


        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            _mpvDll = ObjCRuntime.Dlfcn.dlopen("/Users/zhoukaiwen/Downloads/videolibs/libmpv.1.25.0.dylib", 0);

            if (_mpvDll == IntPtr.Zero)
            {
                Debug.WriteLine("Load mpv failed method");
            }

            _mpvCreate = (MpvCreate)GetDllType(typeof(MpvCreate), "mpv_create");
            _mpvInitialize = (MpvInitialize)GetDllType(typeof(MpvInitialize), "mpv_initialize");
            _mpvTerminateDestroy = (MpvTerminateDestroy)GetDllType(typeof(MpvTerminateDestroy), "mpv_terminate_destroy");
            _mpvCommand = (MpvCommand)GetDllType(typeof(MpvCommand), "mpv_command");
            _mpvSetOption = (MpvSetOption)GetDllType(typeof(MpvSetOption), "mpv_set_option");
            _mpvSetOptionString = (MpvSetOptionString)GetDllType(typeof(MpvSetOptionString), "mpv_set_option_string");
            _mpvGetPropertyString = (MpvGetPropertystring)GetDllType(typeof(MpvGetPropertystring), "mpv_get_property");
            _mpvSetProperty = (MpvSetProperty)GetDllType(typeof(MpvSetProperty), "mpv_set_property");
            _mpvFree = (MpvFree)GetDllType(typeof(MpvFree), "mpv_free");

            _mpvHandle = _mpvCreate.Invoke();
            if (_mpvHandle == IntPtr.Zero) {
                Debug.WriteLine("Create MPV Failed");
            } else {
                Debug.WriteLine("Create MPV");
            }
                
            
            // Do any additional setup after loading the view.
        }

        public void Pause()
        {
            if (_mpvHandle == IntPtr.Zero)
                return;

            var bytes = GetUtf8Bytes("yes");
            _mpvSetProperty(_mpvHandle, GetUtf8Bytes("pause"), MpvFormatString, ref bytes);
        }

        private void Play()
        {
            if (_mpvHandle == IntPtr.Zero)
                return;

            var bytes = GetUtf8Bytes("no");
            _mpvSetProperty(_mpvHandle, GetUtf8Bytes("pause"), MpvFormatString, ref bytes);
        }

        public bool IsPaused()
        {
            if (_mpvHandle == IntPtr.Zero)
                return true;

            var lpBuffer = IntPtr.Zero;
            _mpvGetPropertyString(_mpvHandle, GetUtf8Bytes("pause"), MpvFormatString, ref lpBuffer);
            var isPaused = Marshal.PtrToStringAnsi(lpBuffer) == "yes";
            _mpvFree(lpBuffer);
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
            _mpvCommand(_mpvHandle, mainPtr);
            foreach (var ptr in byteArrayPointers)
            {
                Marshal.FreeHGlobal(ptr);
            }
            Marshal.FreeHGlobal(mainPtr);
        }


        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }
            set
            {
                base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
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
                    Debug.WriteLine("We have url: {0}", path, null);

                }
            }
        }
    }
}
