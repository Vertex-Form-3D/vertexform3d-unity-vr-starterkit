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

    /// <summary>Current config in use. Null if not assigned.</summary>
    public UILayoutConfig Config => uiLayoutConfig;

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
        if (uiLayoutConfig.mainSectionPanels != null && uiLayoutConfig.mainSectionPanels.Count > 0)
        {
            if (logoImage != null)
                logoImage.sprite = uiLayoutConfig.mainSectionPanels[0].logoImage;
            if (backgroundImage != null)
                backgroundImage.sprite = uiLayoutConfig.mainSectionPanels[0].backgroundImage;
        }

        if (leftSectionTitleText != null && !string.IsNullOrEmpty(uiLayoutConfig.leftSectionText))
            leftSectionTitleText.text = uiLayoutConfig.leftSectionText;
    }

    /// <summary>
    /// Call to refresh layout from config at runtime (e.g. after changing config).
    /// </summary>
    public void RefreshFromConfig()
    {
        ApplyLayoutFromConfig();
    }
}
