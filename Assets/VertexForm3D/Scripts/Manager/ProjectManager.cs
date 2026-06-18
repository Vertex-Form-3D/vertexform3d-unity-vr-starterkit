using UnityEngine;
[DefaultExecutionOrder(-100000)]
public class ProjectManager : MonoBehaviour
{
    public UILayoutConfig uiLayoutConfig;
    public Platforms platforms;
    public SettingsUISO settingsUI;

    public static string UserName;
    public static ProjectManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// When false, the session-list lobby runner is off and UI should not show per-world player counts (saves one CCU).
    /// If SettingsUI is not assigned, defaults to true so existing projects keep lobby behavior.
    /// </summary>
    public static bool UsesPhotonSessionLobbyRunner =>
        instance == null || instance.settingsUI == null ||
        instance.settingsUI.photonCcuAllocation == PhotonCcuAllocation.SessionLobbyAndPlayerCounts;
}
