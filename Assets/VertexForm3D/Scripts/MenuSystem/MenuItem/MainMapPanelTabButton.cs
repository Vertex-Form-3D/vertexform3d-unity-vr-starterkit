using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wires a bottom-nav tab button to a menu screen via <see cref="MenuManager"/>.
/// </summary>
[RequireComponent(typeof(Button))]
public class MainMapPanelTabButton : MonoBehaviour
{
    [SerializeField] MenuManager menuManager;
    [SerializeField] GameObject targetScreen;

    void Awake()
    {
        ResolveTargetScreen();
        WireButton();
    }

    void ResolveTargetScreen()
    {
        if (targetScreen != null)
            return;

        var marker = GetComponent<UILayoutCustomPanelMarker>();
        if (marker != null && marker.linkedScreen != null)
            targetScreen = marker.linkedScreen;
    }

    public void WireButton()
    {
        if (menuManager == null)
            menuManager = GetComponentInParent<MenuManager>();

        var button = GetComponent<Button>();
        if (button == null || menuManager == null || targetScreen == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OpenTargetScreen);
    }

    void OpenTargetScreen()
    {
        menuManager.HandleScreen(targetScreen);
        targetScreen.GetComponent<WorldScreen>()?.OnPanelOpened();
    }

    public void SetTargets(MenuManager manager, GameObject screen)
    {
        menuManager = manager;
        targetScreen = screen;
        ResolveTargetScreen();
        WireButton();
    }

    /// <summary>Places tabs use the same screen-switch flow as other panels.</summary>
    public void SetPlacesPanel(int listIndex, MenuManager manager, GameObject screen)
    {
        SetTargets(manager, screen);
    }
}
