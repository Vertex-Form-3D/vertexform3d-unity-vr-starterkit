using UnityEngine;
using UnityEngine.UI;

namespace VertexForm3D.UI
{
    /// <summary>
    /// Drives a CanvasScaler so the reference resolution always fits the screen
    /// without being squashed on ultrawide or shrunk on narrow/portrait screens.
    ///
    /// Add this next to a CanvasScaler whose UiScaleMode is "Scale With Screen Size".
    /// It auto-adjusts MatchWidthOrHeight every frame the screen size changes:
    ///   - screen wider than reference  -> match height (1)
    ///   - screen narrower than reference -> match width (0)
    /// This mirrors Unity's built-in "Expand" match mode but lets you keep the
    /// "MatchWidthOrHeight" mode and still tweak it per-scene if needed.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class ResponsiveCanvasScaler : MonoBehaviour
    {
        [Tooltip("If true, also forces a LayoutRebuild on the target root when the screen size changes. " +
                 "Use when nested HorizontalLayoutGroup / ContentSizeFitter combinations don't update on their own.")]
        public bool rebuildLayoutOnResize = true;

        [Tooltip("Optional root to rebuild. Defaults to this Canvas's RectTransform.")]
        public RectTransform layoutRoot;

        CanvasScaler _scaler;
        Vector2Int _lastScreen;

        void OnEnable()
        {
            _scaler = GetComponent<CanvasScaler>();
            if (layoutRoot == null) layoutRoot = transform as RectTransform;
            Apply(force: true);
        }

        void Update()
        {
            var current = new Vector2Int(Screen.width, Screen.height);
            if (current == _lastScreen) return;
            Apply(force: false);
        }

        void Apply(bool force)
        {
            if (_scaler == null) return;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            float refAspect    = _scaler.referenceResolution.x / Mathf.Max(1f, _scaler.referenceResolution.y);
            float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);

            // Logarithmic blend gives a smooth transition rather than a hard 0/1 switch.
            // log2(screen/ref) maps "same aspect" to 0; positive = wider, negative = narrower.
            // Clamp to [0,1] so it behaves like Unity's MatchWidthOrHeight slider.
            float t = Mathf.Clamp01(0.5f + 0.5f * Mathf.Log(screenAspect / refAspect, 2f));

            _scaler.uiScaleMode      = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.screenMatchMode  = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler.matchWidthOrHeight = t;

            _lastScreen = new Vector2Int(Screen.width, Screen.height);

            if (rebuildLayoutOnResize && layoutRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        }
    }
}
