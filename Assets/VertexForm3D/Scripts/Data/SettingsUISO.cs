using UnityEngine;

/// <summary>
/// Photon billing: a dedicated NetworkRunner that only joins the session lobby consumes an extra CCU.
/// </summary>
public enum PhotonCcuAllocation
{
    [InspectorName("10 CCU — session lobby + live player counts")]
    SessionLobbyAndPlayerCounts,
    [InspectorName("20 CCU — no lobby runner; hide player counts")]
    GameSessionsOnly
}

[CreateAssetMenu(fileName = "SettingsUI", menuName = "Scriptable Objects/SettingsUI")]
public class SettingsUISO : ScriptableObject
{
    public string anonymousUserNamePrefix = "Mystery Guest_";
    public SettingClass defaultSettings = new SettingClass();

    [Tooltip("Session lobby uses a second NetworkRunner (one CCU) so the world list can show live player counts. Game-sessions-only avoids that runner to free a CCU for more concurrent players.")]
    public PhotonCcuAllocation photonCcuAllocation = PhotonCcuAllocation.SessionLobbyAndPlayerCounts;
    [HideInInspector]
    public string addressableCatalogFilePath = "";
    [HideInInspector]
    public string addressableCatalogFileName = "VertexForm3DAddressablesCatalog";
    public bool onlyLocalBundles = true;
}
