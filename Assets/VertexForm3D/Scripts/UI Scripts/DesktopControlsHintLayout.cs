using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the desktop controls hint (the W A S D keys, with "LMB + Drag: Rotate" right under them)
/// small, in the lower-left corner, and behind every other piece of UI.
///
/// The hint is authored in several places — the base scene's DesktopUI and the home scene's
/// MainMenu — each with its own position, and both sat over the bottom left, where the map and
/// the menus open. This rearranges whichever copies exist at runtime, so no scene or prefab has
/// to be edited and a framework update does not undo it.
///
/// "Behind" is done with a nested canvas sorted below everything else on screen: the map,
/// menus and buttons are drawn on top of it, and it never takes a click.
/// </summary>
public class DesktopControlsHintLayout : MonoBehaviour
{
    /// <summary>Size of the hint compared with how it was authored.</summary>
    public static float Scale = 0.6f;

    /// <summary>Gap from the screen's left and bottom edges, in canvas units.</summary>
    public static Vector2 Margin = new Vector2(24f, 24f);

    /// <summary>Sorting order of the hint's canvas: below every other screen canvas.</summary>
    public const int SortingOrder = -100;

    const string KeysName = "WASD Controls";
    const string HostName = "Controls Hint (lower left)";

    readonly HashSet<int> _arranged = new HashSet<int>();
    float _timer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        var go = new GameObject("[Desktop Controls Hint Layout]");
        DontDestroyOnLoad(go);
        go.AddComponent<DesktopControlsHintLayout>();
    }

    void Update()
    {
        // Scenes and menus come and go, each bringing its own copy of the hint; a light check
        // once a second catches every one of them.
        _timer -= Time.unscaledDeltaTime;
        if (_timer > 0f)
            return;

        _timer = 1f;

        foreach (PlatformGameObjectActivator activator in
                 FindObjectsByType<PlatformGameObjectActivator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (activator == null || activator.name != KeysName)
                continue;

            var keys = activator.transform as RectTransform;
            if (keys == null || keys.parent == null || _arranged.Contains(keys.GetInstanceID()))
                continue;

            _arranged.Add(keys.GetInstanceID());

            if (keys.parent.name != HostName)
                Arrange(keys);
        }
    }

    static void Arrange(RectTransform keys)
    {
        var parent = keys.parent as RectTransform;
        if (parent == null)
            return;

        RectTransform mouseHint = FindSiblingWithText(parent, "LMB");
        RectTransform grabHint = FindSiblingNamed(parent, "G");

        // One small panel in the corner holds them all.
        var hostGo = new GameObject(HostName, typeof(RectTransform));
        var host = hostGo.GetComponent<RectTransform>();
        host.SetParent(parent, false);
        host.anchorMin = new Vector2(0f, 0f);
        host.anchorMax = new Vector2(0f, 0f);
        host.pivot = new Vector2(0f, 0f);

        // Margins are measured on screen, so undo any scale the parent applies.
        float parentScale = Mathf.Max(0.01f, parent.localScale.x);
        host.anchoredPosition = Margin / parentScale;
        host.localScale = Vector3.one * Scale;

        float keysWidth = keys.rect.width;
        float keysHeight = keys.rect.height;
        float mouseWidth = mouseHint != null ? mouseHint.rect.width : 0f;
        float mouseHeight = mouseHint != null ? mouseHint.rect.height : 0f;
        const float gap = 8f;

        // Keys on top, the mouse hint tucked right under them.
        host.sizeDelta = new Vector2(Mathf.Max(keysWidth, mouseWidth),
                                     keysHeight + (mouseHint != null ? gap + mouseHeight : 0f));

        // Its own canvas, sorted below everything else, so any UI opened over it is in front.
        var canvas = hostGo.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;

        keys.SetParent(host, false);
        keys.anchorMin = keys.anchorMax = new Vector2(0f, 1f);
        keys.pivot = new Vector2(0f, 1f);
        keys.anchoredPosition = Vector2.zero;

        if (mouseHint != null)
        {
            mouseHint.SetParent(host, false);
            mouseHint.anchorMin = mouseHint.anchorMax = new Vector2(0f, 0f);
            mouseHint.pivot = new Vector2(0f, 0f);
            mouseHint.anchoredPosition = Vector2.zero;
        }

        // The "G - Grab item" hint appears now and then; it sits just above the rest.
        if (grabHint != null)
        {
            grabHint.SetParent(host, false);
            grabHint.anchorMin = grabHint.anchorMax = new Vector2(0f, 1f);
            grabHint.pivot = new Vector2(0f, 0f);
            grabHint.anchoredPosition = new Vector2(0f, gap);
        }

        // A hint never takes a click meant for the world or the UI.
        foreach (Graphic graphic in hostGo.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        Debug.Log($"[DesktopControlsHint] Moved the controls hint under '{parent.name}' to the lower left, " +
                  $"at {Scale:P0} size and behind the rest of the UI.");
    }

    static RectTransform FindSiblingWithText(RectTransform parent, string contains)
    {
        foreach (Transform child in parent)
        {
            TMP_Text text = child.GetComponentInChildren<TMP_Text>(true);
            if (text != null && text.text != null && text.text.Contains(contains))
                return child as RectTransform;
        }

        return null;
    }

    static RectTransform FindSiblingNamed(RectTransform parent, string name)
    {
        foreach (Transform child in parent)
            if (child.name == name)
                return child as RectTransform;

        return null;
    }
}
