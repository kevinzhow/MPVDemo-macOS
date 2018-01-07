using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Drawing;

using static MPVDemo.libmpv;

using System.Diagnostics;

namespace MPVDemo
{
    public delegate void MpvBoolPropChangeHandler(string propName, bool value);

    public class mpv
    {
        public  event Action Shutdown;
        public  event Action AfterShutdown;
        public  event Action FileLoaded;
        public  event Action PlaybackRestart;
        public  List<Action<bool>> BoolPropChangeActions = new List<Action<bool>>();
        public  IntPtr MpvHandle;
        public  IntPtr MpvWindowHandle;
        public  string BR2 = Environment.NewLine + Environment.NewLine;

        public mpv(IntPtr windowHandle)
        {
            MpvHandle = mpv_create();

            MpvWindowHandle = windowHandle;

            SetIntProp("input-ar-delay", 500);
            SetIntProp("input-ar-rate", 20);
            SetIntProp("volume", 50);
            SetStringProp("hwdec", "auto");
            SetStringProp("input-default-bindings", "yes");
            //SetStringProp("opengl-backend", "angle");
            SetStringProp("osd-playing-msg", "'${filename}'");
            SetStringProp("profile", "opengl-hq");
            SetStringProp("screenshot-directory", "~~desktop/");
            SetStringProp("vo", "opengl");
            SetStringProp("keep-open", "always");
            SetStringProp("keep-open-pause", "no");
            SetStringProp("osc", "yes");
            SetStringProp("config", "yes");
            SetStringProp("wid", MpvWindowHandle.ToString());
            SetStringProp("force-window", "yes");
            mpv_initialize(MpvHandle);
            Task.Run(() => { EventLoop(); });

        }

        public void EventLoop()
        {
            while (true)
            {
                IntPtr ptr = mpv_wait_event(MpvHandle, -1);
                mpv_event evt = (mpv_event)Marshal.PtrToStructure(ptr, typeof(mpv_event));
                Debug.WriteLine(evt.event_id);

                switch (evt.event_id)
                {
                    case mpv_event_id.MPV_EVENT_SHUTDOWN:
                        Shutdown?.Invoke();
                        AfterShutdown?.Invoke();
                        return;
                    case mpv_event_id.MPV_EVENT_FILE_LOADED:
                        FileLoaded?.Invoke();
                        break;
                    case mpv_event_id.MPV_EVENT_PLAYBACK_RESTART:
                        PlaybackRestart?.Invoke();
                        var s = new Size(GetIntProp("dwidth"), GetIntProp("dheight"));


                        break;
                    case mpv_event_id.MPV_EVENT_PROPERTY_CHANGE:
                        var eventData = (mpv_event_property)Marshal.PtrToStructure(evt.data, typeof(mpv_event_property));

                        if (eventData.format == mpv_format.MPV_FORMAT_FLAG)
                            foreach (var action in BoolPropChangeActions)
                                action.Invoke(Marshal.PtrToStructure<int>(eventData.data) == 1);
                        break;
                }
            }
        }

        public void Command(params string[] args)
        {
            if (MpvHandle == IntPtr.Zero)
                return;

            IntPtr[] byteArrayPointers;
            var mainPtr = AllocateUtf8IntPtrArrayWithSentinel(args, out byteArrayPointers);
            int err = mpv_command(MpvHandle, mainPtr);

            if (err < 0)
                throw new Exception($"{(mpv_error)err}");

            foreach (var ptr in byteArrayPointers)
                Marshal.FreeHGlobal(ptr);

            Marshal.FreeHGlobal(mainPtr);
        }

        public void CommandString(string command, bool throwException = true)
        {
            if (MpvHandle == IntPtr.Zero)
                return;

            int err = mpv_command_string(MpvHandle, command);

            if (err < 0 && throwException)
                throw new Exception($"{(mpv_error)err}" + BR2 + command);
        }

        public void SetStringProp(string name, string value, bool throwException = true)
        {
            var bytes = GetUtf8Bytes(value);
            int err = mpv_set_property(MpvHandle, GetUtf8Bytes(name), mpv_format.MPV_FORMAT_STRING, ref bytes);

            if (err < 0 && throwException)
                throw new Exception($"{name}: {(mpv_error)err}");
        }

        public string GetStringProp(string name)
        {
            var lpBuffer = IntPtr.Zero;
            int err = mpv_get_property(MpvHandle, GetUtf8Bytes(name), mpv_format.MPV_FORMAT_STRING, ref lpBuffer);
            var ret = StringFromNativeUtf8(lpBuffer);
            mpv_free(lpBuffer);

            if (err < 0)
                throw new Exception($"{name}: {(mpv_error)err}");

            return ret;
        }

        public int GetIntProp(string name, bool throwException = true)
        {
            var lpBuffer = IntPtr.Zero;
            int err = mpv_get_property(MpvHandle, GetUtf8Bytes(name), mpv_format.MPV_FORMAT_INT64, ref lpBuffer);

            if (err < 0 && throwException)
                throw new Exception($"{name}: {(mpv_error)err}");
            else
                return lpBuffer.ToInt32();
        }

        public void SetIntProp(string name, int value)
        {
            Int64 val = value;
            int err = mpv_set_property(MpvHandle, GetUtf8Bytes(name), mpv_format.MPV_FORMAT_INT64, ref val);

            if (err < 0)
                throw new Exception($"{name}: {(mpv_error)err}");
        }

        public void ObserveBoolProp(string name, Action<bool> action)
        {
            BoolPropChangeActions.Add(action);
            int err = mpv_observe_property(MpvHandle, (ulong)action.GetHashCode(), name, mpv_format.MPV_FORMAT_FLAG);

            if (err < 0)
                throw new Exception($"{name}: {(mpv_error)err}");
        }

        public void UnobserveBoolProp(string name, Action<bool> action)
        {
            BoolPropChangeActions.Remove(action);
            int err = mpv_unobserve_property(MpvHandle, (ulong)action.GetHashCode());

            if (err < 0)
                throw new Exception($"{name}: {(mpv_error)err}");
        }

        public void LoadFile(string file)
        {
            this.Command("loadfile", file);
        }

        public void Pause()
        {
            
            if(GetStringProp("pause") == "no") {
                SetStringProp("pause", "yes");
            }
        }


        public IntPtr AllocateUtf8IntPtrArrayWithSentinel(string[] arr, out IntPtr[] byteArrayPointers)
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

        public string[] NativeUtf8StrArray2ManagedStrArray(IntPtr pUnmanagedStringArray, int StringCount)
        {
            IntPtr[] pIntPtrArray = new IntPtr[StringCount];
            string[] ManagedStringArray = new string[StringCount];
            Marshal.Copy(pUnmanagedStringArray, pIntPtrArray, 0, StringCount);

            for (int i = 0; i < StringCount; i++)
                ManagedStringArray[i] = StringFromNativeUtf8(pIntPtrArray[i]);

            return ManagedStringArray;
        }

        public string StringFromNativeUtf8(IntPtr nativeUtf8)
        {
            int len = 0;
            while (Marshal.ReadByte(nativeUtf8, len) != 0) ++len;
            byte[] buffer = new byte[len];
            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        public byte[] GetUtf8Bytes(string s) => Encoding.UTF8.GetBytes(s + "\0");
    }
}