using UnityEngine;
using TMPro;
public class PlayerJoinUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMeshProUGUI;
    void Start()
    {
        //Destroy(gameObject, 2f);
    }
    public void SetPlayerText(string playerName, bool isJoined)
    {
        if (isJoined)
        {
            textMeshProUGUI.color = Color.green;
            textMeshProUGUI.text = $"{playerName} joined the world";
        }
        else
        {
            textMeshProUGUI.color = Color.red;
            textMeshProUGUI.text = $"{playerName} left the world";
        }
    }

}
