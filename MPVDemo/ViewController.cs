using System;
using System.Runtime.InteropServices;
using AppKit;
using Foundation;
using ObjCRuntime;
using System.Diagnostics;

namespace MPVDemo
{
    public partial class ViewController : NSViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        private IntPtr _mpvHandle;
        private IntPtr _mpvDll;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr MpvCreate();
        private MpvCreate _mpvCreate;

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
            _mpvHandle = _mpvCreate.Invoke();
            if (_mpvHandle == IntPtr.Zero) {
                Debug.WriteLine("Create MPV Failed");
            } else {
                Debug.WriteLine("Create MPV");
            }
                
            
            // Do any additional setup after loading the view.
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
    }
}
