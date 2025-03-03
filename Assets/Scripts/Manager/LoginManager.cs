using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.SceneManagement;

namespace VertextFormCore
{
    public class LoginManager : MonoBehaviour
    {
        public TMP_InputField PlayerName_InputName;


        public void ConnectAnonymously()
        {
            ConnectToPhotonServer();
        }

        public void ConnectToPhotonServer()
        {
            if (PlayerName_InputName != null)
            {
                PhotonNetwork.LocalPlayer.NickName = !string.IsNullOrEmpty(PlayerName_InputName.text) ? PlayerName_InputName.text : "Mystery Guest_" + Random.Range(1111, 9999);
                SceneManager.LoadScene("HomeScene");
            }
        }
    }
}