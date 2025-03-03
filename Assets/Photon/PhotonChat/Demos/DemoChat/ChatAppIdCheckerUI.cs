// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Exit Games GmbH"/>
// <summary>Demo code for Photon Chat in Unity.</summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------


using UnityEngine;


using UnityEngine.UI;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

namespace Photon.Chat.Demo
{
    /// <summary>
    /// This is used in the Editor Splash to properly inform the developer about the chat AppId requirement.
    /// </summary>
    [ExecuteInEditMode]
    public class ChatAppIdCheckerUI : MonoBehaviour
    {
        public Text Description;
        public bool WizardOpenedOnce;   // avoid opening the wizard again and again

        // TODO: maybe this can run on Start(), not on Update()?!
        public void Update()
        {
            bool showWarning = false;
            string descriptionText = string.Empty;

            #if PHOTON_UNITY_NETWORKING
            showWarning = string.IsNullOrEmpty(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat);
            if (showWarning)
            {
                descriptionText = "<Color=Red>WARNING:</Color>\nPlease setup a Chat AppId in the PhotonServerSettings file.";
            }
            #else
            #if UNITY_6000_0_OR_NEWER
            ChatGui cGui = FindFirstObjectByType<ChatGui>(); // this could be a serialized reference instead of finding this each time
            #else
            ChatGui cGui = FindObjectOfType<ChatGui>(); // this could be a serialized reference instead of finding this each time
            #endif

            showWarning = cGui == null || string.IsNullOrEmpty(cGui.chatAppSettings.AppIdChat);
            if (showWarning)
            {
                descriptionText = "<Color=Red>Please setup the Chat AppId.\nOpen the setup panel: Window, Photon Chat, Setup.</Color>";
                
                #if UNITY_EDITOR
                if (!WizardOpenedOnce)
                {
                    WizardOpenedOnce = true;
                    UnityEditor.EditorApplication.ExecuteMenuItem("Window/Photon Chat/Setup");
                }
                #endif
            }
            #endif

            this.Description.text = descriptionText;
        }
    }
}