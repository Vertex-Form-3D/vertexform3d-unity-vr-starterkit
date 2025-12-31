using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplayerMessageHandler : MonoBehaviour, IPlayerJoined, IPlayerLeft
{
    public GameObject messagePrefab;

    void Start()
    {

    }

    public void PlayerJoined(PlayerRef player)
    {
        // Get player name from Fusion player data or use default
        string playerName = "Player " + player.PlayerId;

        // Try to get stored player name if available

        ShowMessage(playerName + " Joined the World.", "#93FF00");
    }

    public void PlayerLeft(PlayerRef player)
    {
        // Get player name from Fusion player data or use default
        string playerName = "Player " + player.PlayerId;
        ShowMessage(playerName + " Left the World.", "#FF0000");
    }

    public void ShowMessage(string message, string colorCode = "#FF0000")
    {
        GameObject m = Instantiate(messagePrefab, transform);
        m.GetComponent<MessageScript>().ShowMessage(message, GetColorFromCode(colorCode));
    }

    Color GetColorFromCode(string colorCode)
    {
        Color colorFromHex = Color.black;
        ColorUtility.TryParseHtmlString(colorCode, out colorFromHex);
        return colorFromHex;
    }
}
