using UnityEngine;
using WebXR;

// This assembly is WebGL-only (see VertexForm.WebXRBridge.asmdef). It is not loaded in Editor or standalone/mobile builds.
namespace VertexForm.WebXRBridge
{
    /// <summary>
    /// <b>WebGL WebXR browser only.</b> Reads Quest-style face buttons from de-panther WebXR Export
    /// (<c>WebXRManager.OnControllerUpdate</c>), which maps browser gamepad A/B/X/Y into
    /// <see cref="WebXRControllerData.buttonA"/> / <see cref="WebXRControllerData.buttonB"/>.
    /// </summary>
    public static class WebXRFaceButtonInput
    {
        private const float PressThreshold = 0.5f;

        private static float s_rightB;
        private static float s_leftB;
        private static float s_rightA;
        private static float s_leftA;
        private static bool s_hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            if (s_hooked)
                return;
            s_hooked = true;
            WebXRManager.OnControllerUpdate += OnControllerUpdate;
        }

        private static void OnControllerUpdate(WebXRControllerData d)
        {
            if (d.hand == (int)WebXRControllerHand.RIGHT)
            {
                if (!d.enabled)
                {
                    s_rightA = 0f;
                    s_rightB = 0f;
                    return;
                }

                s_rightA = d.buttonA;
                s_rightB = d.buttonB;
                return;
            }

            if (d.hand == (int)WebXRControllerHand.LEFT)
            {
                if (!d.enabled)
                {
                    s_leftA = 0f;
                    s_leftB = 0f;
                    return;
                }

                s_leftA = d.buttonA;
                s_leftB = d.buttonB;
            }
        }

        /// <summary>Right controller <b>B</b> (secondary on Quest Touch).</summary>
        public static bool IsRightButtonBHeld() => s_rightB >= PressThreshold;

        /// <summary>Left controller <b>Y</b> (secondary on Quest Touch).</summary>
        public static bool IsLeftButtonBHeld() => s_leftB >= PressThreshold;

        /// <summary>Right <b>A</b> / left <b>X</b> (primary).</summary>
        public static bool IsRightButtonAHeld() => s_rightA >= PressThreshold;

        public static bool IsLeftButtonAHeld() => s_leftA >= PressThreshold;
    }
}
