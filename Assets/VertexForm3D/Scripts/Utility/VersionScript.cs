using TMPro;
using UnityEngine;

public class VersionScript : MonoBehaviour
{
    public TMP_Text versionText;
    void Start()
    {
        if (versionText != null)
        {
            versionText.text = $"Version: {Application.version}";
        }
    }

}
