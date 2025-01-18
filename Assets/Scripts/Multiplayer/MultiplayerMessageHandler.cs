using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplayerMessageHandler : MonoBehaviourPunCallbacks
{
    public GameObject messagePrefab;
    void Start()
    {

    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        ShowMessage(newPlayer.NickName + "Joined the World.", "#93FF00");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ShowMessage(otherPlayer.NickName + "Left the World.", "#FF0000");
    }
    public void ShowMessage(string message,string colorCode = "#FF0000")
    {
        GameObject m = Instantiate(messagePrefab, transform);
        m.GetComponent<MessageScript>().ShowMessage(message, GetColorFromCode(colorCode));
    }

    Color GetColorFromCode(string colorCode)
    {
        Color colorFromHex=Color.black;
        ColorUtility.TryParseHtmlString(colorCode, out colorFromHex);
        return colorFromHex;
    }
}
