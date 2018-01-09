using System;
using System.Runtime.InteropServices;

namespace DylibLoader
{
    internal static class MacNativeMethods
    {
        public const int RTLD_NOW = 0x002;

        public static  IntPtr dlsym(IntPtr handle, string symbol) {
            return ObjCRuntime.Dlfcn.dlsym(handle, symbol);
        }


        public static  IntPtr dlopen(string fileName, int flag) {
            return ObjCRuntime.Dlfcn.dlopen(fileName, flag);
        }
    }
}