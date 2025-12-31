#if UNITY_WEBGL && UNITY_2021_2_OR_NEWER // requires ES6
using System;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;

namespace Photon.Voice.Unity
{
    // Returns the result asynchronously, calls OnReady when done.
    // Like other platforms enumerators, starts enumeration in constructor.
    // OnReady is called immediately when set if the list is already available.
    public class WebDeviceEnumerator : DeviceEnumeratorBase
    {
        const string lib_name = "__Internal";

        [DllImport(lib_name, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int PhotonVoice_WebRTC_EnumerateDevices(int requestId, string kind, Action<int, int, IntPtr, int> resultCallbackStatic);

        static int requestIdCnt;
        static Dictionary<int, WebDeviceEnumerator> requests = new Dictionary<int, WebDeviceEnumerator>();

        [MonoPInvokeCallbackAttribute(typeof(Action<int, IntPtr, int>))]
        static void resultCallbackStatic(int requestId, int err, IntPtr ptr, int count)
        {
            requests[requestId].resultCallback(err, ptr, count);
            requests.Remove(requestId);
        }

        void resultCallback(int err, IntPtr ptr, int count)
        {
            if (err != 0)
            {
                Error = "Can't create Enumerator: " + err;
            }
            else
            {
                var ptrMan = new IntPtr[count];
                Marshal.Copy(ptr, ptrMan, 0, count);
                // [id1, lbl1, id2, lbl2,...]
                devices = ptrMan.Select((x, i) => new { Str = x, Ind = i }).GroupBy(x => x.Ind / 2, x => x.Str).Select(x => new DeviceInfo(Marshal.PtrToStringUTF8(x.First()), Marshal.PtrToStringUTF8(x.ElementAt(1)))).ToList();

                logger.Log(LogLevel.Info, "[PV] WebDeviceEnumerator " +  filter + ": refreshed");
            }

            if (OnReady != null)
            {
                OnReady();
            }
        }

        public WebDeviceEnumerator(ILogger logger, string filter) : base(logger)
        {
            this.filter = filter;
            logger.Log(LogLevel.Info, "[PV] WebDeviceEnumerator " + filter + ": created");
            Refresh();
        }

        string filter;

        public override void Refresh()
        {
            int reqId = ++requestIdCnt;
            requests[reqId] = this;
            PhotonVoice_WebRTC_EnumerateDevices(requestIdCnt, filter, resultCallbackStatic);
        }

        public override void Dispose()
        {
        }
    }

    public class WebAudioInEnumerator : WebDeviceEnumerator { public WebAudioInEnumerator(ILogger logger) : base(logger, "audioinput") { } }
    public class WebVideoInEnumerator : WebDeviceEnumerator { public WebVideoInEnumerator(ILogger logger) : base(logger, "videoinput") { } }
}
#endif