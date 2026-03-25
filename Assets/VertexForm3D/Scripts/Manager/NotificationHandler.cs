using UnityEngine;

public class NotificationHandler : MonoBehaviour
{
    public GameObject notificationPrefab;
    public Transform notificationContainerDesktop;
    public Transform notificationContainerVR;

    void Start()
    {

    }
    public void ShowMessage(string message, string colorCode = "#FF0000")
    {
        if (ProjectManager.instance.platforms.platformChoice == platform.Desktop)
        {
            GameObject m = Instantiate(notificationPrefab, notificationContainerDesktop);
            m.GetComponent<MessageScript>().ShowMessage(message, GetColorFromCode(colorCode));
        }
        else
        {
            GameObject m = Instantiate(notificationPrefab, notificationContainerVR);
            m.GetComponent<MessageScript>().ShowMessage(message, GetColorFromCode(colorCode));
        }
    }
    Color GetColorFromCode(string colorCode)
    {
        Color colorFromHex = Color.black;
        ColorUtility.TryParseHtmlString(colorCode, out colorFromHex);
        return colorFromHex;
    }

}
