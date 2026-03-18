using UnityEngine;

[CreateAssetMenu(fileName = "SettingsUI", menuName = "Scriptable Objects/SettingsUI")]
public class SettingsUISO : ScriptableObject
{
    public string anonymousUserNamePrefix = "Mystery Guest_";
    public SettingClass defaultSettings = new SettingClass();
    [HideInInspector]
    public string addressableCatalogFilePath = "";
    [HideInInspector]
    public string addressableCatalogFileName = "VertexForm3DAddressablesCatalog";
}
