using UnityEngine;

/// <summary>
/// Marks menu screens and bottom-nav tabs created from <see cref="UILayoutConfig.mainSectionPanelEntries"/>.
/// Used to find baked custom panels in prefabs and to clean them up on re-apply.
/// </summary>
public class UILayoutCustomPanelMarker : MonoBehaviour
{
    public bool isTabButton;
    public int sortOrder;
    public string panelKey = "";
    public GameObject linkedScreen;
    public GameObject linkedTab;
}
