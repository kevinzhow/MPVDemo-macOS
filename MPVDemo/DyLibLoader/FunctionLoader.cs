using System;
using System.Runtime.InteropServices;

namespace DylibLoader
{
    /// <summary>
    ///     Supports loading functions from native libraries. Provides a more flexible alternative to P/Invoke.
    /// </summary>
    public static class FunctionLoader
    {
        /// <summary>
        ///     Creates a delegate which invokes a native function.
        /// </summary>
        /// <typeparam name="T">
        ///     The function delegate.
        /// </typeparam>
        /// <param name="nativeLibraryHandle">
        ///     The native library which contains the function.
        /// </param>
        /// <param name="functionName">
        ///     The name of the function for which to create the delegate.
        /// </param>
        /// <returns>
        ///     A new delegate which points to the native function.
        /// </returns>
        internal static T GetFunctionDelegate<T>(IntPtr nativeLibraryHandle, string functionName, bool throwOnError = true)
        {
            var ptr = GetFunctionPointer(nativeLibraryHandle, functionName);

            if (ptr == IntPtr.Zero)
            {
                if (throwOnError) throw new EntryPointNotFoundException($"Could not find the entrypoint for {functionName}.");
                return default(T);
            }


            try
            {
                return Marshal.GetDelegateForFunctionPointer<T>(ptr);
            }
            catch (MarshalDirectiveException)
            {
                if (throwOnError)
                    throw;
                return default(T);
            }
        }

        private static IntPtr GetFunctionPointer(IntPtr nativeLibraryHandle, string functionName)
        {

            return MacNativeMethods.dlsym(nativeLibraryHandle, functionName);
        }
    }
}