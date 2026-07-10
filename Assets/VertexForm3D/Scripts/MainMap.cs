using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Base component that reads <see cref="UILayoutConfig"/> and applies it to actual UI elements in the scene.
/// Assign the config asset and optional section roots; override ApplyLayoutFromConfig to customize per-scene.
/// </summary>
public class MainMap : MonoBehaviour
{
    [Tooltip("Main UI Database / layout config. Drives section visibility and content.")]
    [SerializeField] protected UILayoutConfig uiLayoutConfig;

    [Header("Optional section roots (assign to apply visibility from config)")]
    [SerializeField] protected GameObject leftSectionRoot;
    [SerializeField] protected GameObject rightSectionRoot;
    [SerializeField] protected GameObject mirrorRoot;

    [Header("Optional left section text (from config.leftSectionText)")]
    [SerializeField] protected TMP_Text leftSectionTitleText;

    [Header("Optional logo image (from config.logoImage)")]
    [SerializeField] protected Image logoImage;
    [SerializeField] protected Image backgroundImage;

    [Header("Dynamic main panels (optional)")]
    [Tooltip("When assigned, built-in and custom main panels plus bottom-nav tabs are configured from UILayoutConfig.")]
    [SerializeField] protected MenuManager menuManager;

    /// <summary>Current config in use. Null if not assigned.</summary>
    public UILayoutConfig Config => uiLayoutConfig;

    protected virtual void Start()
    {
        ApplyLayoutFromConfig();
    }

    /// <summary>
    /// Applies UILayoutConfig to scene elements. Override to apply panels, avatars, etc.
    /// </summary>
    public virtual void ApplyLayoutFromConfig()
    {
        if (uiLayoutConfig == null) return;

        if (leftSectionRoot != null)
            leftSectionRoot.SetActive(uiLayoutConfig.leftSectionEnabled);

        if (rightSectionRoot != null)
            rightSectionRoot.SetActive(uiLayoutConfig.rightSectionEnabled);
        if (mirrorRoot != null)
            mirrorRoot.SetActive(uiLayoutConfig.mirror);

        uiLayoutConfig.EnsureDefaultPanelEntries();
        int primaryMainIndex = uiLayoutConfig.GetPrimaryMainListIndex();
        var mainPanel = primaryMainIndex >= 0
            ? uiLayoutConfig.mainSectionPanelEntries[primaryMainIndex]
            : null;
        if (mainPanel != null)
        {
            if (logoImage != null)
                logoImage.sprite = mainPanel.logoImage;
            if (backgroundImage != null)
                backgroundImage.sprite = mainPanel.backgroundImage;
        }

        if (leftSectionTitleText != null && !string.IsNullOrEmpty(uiLayoutConfig.leftSectionText))
            leftSectionTitleText.text = uiLayoutConfig.leftSectionText;

        ApplyMainSectionPanels();
    }

    /// <summary>
    /// Configures built-in and custom main panels and bottom navigation from UILayoutConfig.
    /// </summary>
    protected virtual void ApplyMainSectionPanels()
    {
        if (menuManager == null)
            menuManager = GetComponentInChildren<MenuManager>(true);
        if (menuManager == null || uiLayoutConfig == null) return;

        menuManager.ApplyPanelConfig(uiLayoutConfig);
    }

    /// <summary>
    /// Call to refresh layout from config at runtime (e.g. after changing config).
    /// </summary>
    public void RefreshFromConfig()
    {
        ApplyLayoutFromConfig();
    }
}
