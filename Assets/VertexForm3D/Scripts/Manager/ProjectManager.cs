using UnityEngine;

public class ProjectManager : MonoBehaviour
{
    public UILayoutConfig uiLayoutConfig;
    public PlatformAndSettings platformAndSettings;
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
}
