#if UNITY_WEBGL
using AOT;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Photon.Voice.Unity
{
    public class TimerWorker
    {
        const string lib_name = "__Internal";

        [DllImport(lib_name, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int Photon_JS_TimerWorker_Start(Action<int> callback, int interval);

        [DllImport(lib_name, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void Photon_JS_TimerWorker_Stop(int handle);

        int handle;
        static Dictionary<int, TimerWorker> handles = new Dictionary<int, TimerWorker>();
        Action callback;

        public TimerWorker(Action callback, int interval)
        {
            this.callback = callback;
            handle = Photon_JS_TimerWorker_Start(CallbackStatic, interval);
            handles[handle] = this;
        }

        public void Stop()
        {
            Photon_JS_TimerWorker_Stop(handle);
        }

        [MonoPInvokeCallbackAttribute(typeof(Action<int>))]
        public static void CallbackStatic(int handle)
        {
            handles[handle].Callback();
        }

        private void Callback()
        {
            callback();
        }
    }
}
#endif
