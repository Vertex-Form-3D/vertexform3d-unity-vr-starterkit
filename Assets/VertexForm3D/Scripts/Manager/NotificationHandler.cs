using UnityEngine;

public class NotificationHandler : MonoBehaviour
{
    public GameObject notificationPrefab;
    public Transform notificationContainer;
    void Start()
    {
        
    }
    public void ShowMessage(string message, string colorCode = "#FF0000")
    {
        GameObject m = Instantiate(notificationPrefab, notificationContainer);
        m.GetComponent<MessageScript>().ShowMessage(message, GetColorFromCode(colorCode));
    }
    Color GetColorFromCode(string colorCode)
    {
        Color colorFromHex = Color.black;
        ColorUtility.TryParseHtmlString(colorCode, out colorFromHex);
        return colorFromHex;
    }

}
