#if PHOTON_VOICE_VIDEO_ENABLE
// Basic support for https://github.com/hecomi/uWindowCapture

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN) && U_WINDOW_CAPTURE_RECORDER_ENABLE

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using uWindowCapture;

namespace Photon.Voice.Unity
{
    [AddComponentMenu("Photon Voice/uWindowCaptureHost")]
    public class uWindowCaptureHost : MonoBehaviour
    {
        struct Client
        {
            internal int fps;
            internal Action onCaptured;
        }

        Dictionary<object, Client> clients = new Dictionary<object, Client>();

        float updateInt;
        float nextRequestTime;
        private bool waitingCapture = false;

        public bool PrevBufferMode { get; private set; } // detect buffer mode change and re-initialize

        public void Register(object id, int fps, Action onCaptured)
        {
            clients.Add(id, new Client() { fps = fps, onCaptured = onCaptured });
            updateFPS();
        }

        public void Unregister(object id)
        {
            if (clients.TryGetValue(id, out Client c))
            {
                clients.Remove(id);
                updateFPS();
            }
        }

        void updateFPS()
        {
            int fps = clients.Values.Aggregate(2, (m, x) => Math.Max(m, x.fps)); // minimum 2 fps, the capture does not with 1 fps
            updateInt = 1.0f / fps;
        }


        void onCaptured()
        {
            if (!waitingCapture) return;
            waitingCapture = false;
            foreach (var x in clients.Values)
            {
                x.onCaptured();
            }
            // use window buffer if available
            PrevBufferMode = UseWindowBuffer && window.buffer != IntPtr.Zero;
        }

        [SerializeField]
        WindowTextureType type = WindowTextureType.Desktop;
        public WindowTextureType Type
        {
            get
            {
                return type;
            }
            set
            {
                shouldUpdateWindowOnParameterChanged = true;
                type = value;
            }
        }

        [SerializeField]
        bool altTabWindow = false;
        public bool AltTabWindow
        {
            get
            {
                return altTabWindow;
            }
            set
            {
                shouldUpdateWindowOnParameterChanged = true;
                altTabWindow = value;
            }
        }

        [SerializeField]
        string partialWindowTitle;
        public string PartialWindowTitle
        {
            get
            {
                return partialWindowTitle;
            }
            set
            {
                shouldUpdateWindowOnParameterChanged = true;
                partialWindowTitle = value;
            }
        }

        [SerializeField]
        int desktopIndex = 0;
        public int DesktopIndex
        {
            get
            {
                return desktopIndex;
            }
            set
            {
                shouldUpdateWindowOnParameterChanged = true;
                desktopIndex = (UwcManager.desktopCount > 0) ?
                    Mathf.Clamp(value, 0, UwcManager.desktopCount - 1) : 0;
            }
        }

        public CaptureMode captureMode = CaptureMode.Auto;
        // use window buffer if available
        public bool UseWindowBuffer;
        public CapturePriority capturePriority = CapturePriority.Auto;
        public bool drawCursor = true;

        UwcWindow window;
        public UwcWindow Window
        {
            get
            {
                return window;
            }
            set
            {
                if (window == value)
                {
                    return;
                }

                if (window != null)
                {
                    window.onCaptured.RemoveListener(onCaptured);
                }

                var old = window;
                window = value;
                onWindowChanged.Invoke(window, old);

                if (window != null)
                {
                    shouldUpdateWindowOnParameterChanged = false;
                    window.onCaptured.AddListener(onCaptured);
                    window.RequestCapture(CapturePriority.High);
                }
            }
        }

        UwcWindowChangeEvent onWindowChanged = new UwcWindowChangeEvent();
        public UwcWindowChangeEvent OnWindowChanged
        {
            get { return onWindowChanged; }
        }

        protected virtual void Update()
        {
            if (clients.Count == 0)
            {
                return;
            }

            if (SearchTiming == WindowSearchTiming.Always || (SearchTiming == WindowSearchTiming.OnlyWhenParameterChanged && shouldUpdateWindowOnParameterChanged))
            {
                switch (Type)
                {
                    case WindowTextureType.Window:
                        Window = UwcManager.Find(PartialWindowTitle, AltTabWindow);
                        break;
                    case WindowTextureType.Desktop:
                        Window = UwcManager.FindDesktop(DesktopIndex);
                        break;
                    case WindowTextureType.Child:
                        break;
                }
            }

            if (Window != null && Window.isValid)
            {

                Window.cursorDraw = drawCursor;
                Window.captureMode = captureMode;

                if (Time.realtimeSinceStartup < nextRequestTime)
                {
                    return;
                }
                else
                {
                    nextRequestTime = Time.realtimeSinceStartup + updateInt;
                    waitingCapture = true;

                    var priority = capturePriority;
                    if (priority == CapturePriority.Auto)
                    {
                        priority = CapturePriority.Low;
                        if (Window == UwcManager.cursorWindow)
                        {
                            priority = CapturePriority.High;
                        }
                        else if (Window.zOrder < UwcSetting.MiddlePriorityMaxZ)
                        {
                            priority = CapturePriority.Middle;
                        }
                    }

                    Window.RequestCapture(priority);
                }
            }
        }

        private void UWindowCaptureMB_onCaptured()
        {
            throw new NotImplementedException();
        }

        public void RequestWindowUpdate()
        {
            shouldUpdateWindowOnParameterChanged = true;
        }

        bool shouldUpdateWindowOnParameterChanged = true;

        [SerializeField]
        WindowSearchTiming searchTiming = WindowSearchTiming.OnlyWhenParameterChanged;
        public WindowSearchTiming SearchTiming
        {
            get
            {
                return searchTiming;
            }
            set
            {
                searchTiming = value;
                shouldUpdateWindowOnParameterChanged = true;
            }
        }

    }
}

#endif
#endif
