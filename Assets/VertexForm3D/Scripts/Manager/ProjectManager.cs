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
        instance.settingsUI.photonCcuAllocation != PhotonCcuAllocation.GameSessionsOnly;

    /// <summary>
    /// When true, the session-list lobby runner is closed on entering a world, so a player inside a
    /// world holds one Photon connection instead of two.
    ///
    /// It is NOT reopened automatically afterwards — this mirrors what World Expo has been doing,
    /// rather than inventing a restart that has never been tested. So per-world player counts stop
    /// updating from the first world entry until the app is restarted. Worth revisiting if the
    /// counts turn out to matter after returning to the menu.
    ///
    /// Defaults to false — including when SettingsUI is unassigned — so existing projects keep the
    /// lobby running exactly as before.
    /// </summary>
    public static bool ShutsDownLobbyRunnerOnWorldEntry =>
        instance != null && instance.settingsUI != null &&
        instance.settingsUI.photonCcuAllocation == PhotonCcuAllocation.SessionLobbyInMenuOnly;
}
