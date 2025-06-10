using Photon.Pun;
using UnityEngine;

public class ProjectManager : MonoBehaviour
{
    public ProjectDataScriptableObject projectDataSO;

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
    void Start()
    {
        SetUpLogSetting();
    }


    public void SetUpLogSetting()
    {
        Debug.unityLogger.logEnabled = projectDataSO.projectData.DebugEnabled;
    }
}
