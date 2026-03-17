using UnityEngine;

[CreateAssetMenu(fileName = "Platform and Settings", menuName = "ScriptableObjects/Platform and Settings", order = 1)]
public class PlatformAndSettings : ScriptableObject
{
    public platform platformChoice = platform.VR;
    public Mode mode = Mode.player;
    public string platformStepsVR = "1. Ensure XR Rig is in scene.\n2. Configure XR Plugin Management for Quest/PC.\n3. Assign Input Action Manager if using XRI.";
    public string platformStepsDesktop = "1. Use Desktop XR Rig or OrbitCamera.\n2. Disable VR-specific components.\n3. Assign keyboard/mouse input.";
    public string anonymousUserNamePrefix = "Mystery Guest_";
    public SettingClass defaultSettings = new SettingClass();
    [HideInInspector]
    public string addressableCatalogFilePath = "";
    [HideInInspector]
    public string addressableCatalogFileName = "VertexForm3DAddressablesCatalog";
}
