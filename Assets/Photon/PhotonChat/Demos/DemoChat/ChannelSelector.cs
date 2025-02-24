// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Exit Games GmbH"/>
// <summary>Demo code for Photon Chat in Unity.</summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------


using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace Photon.Chat.Demo
{
    public class ChannelSelector : MonoBehaviour, IPointerClickHandler
    {
        public string Channel;

        public void SetChannel(string channel)
        {
            this.Channel = channel;
            Text t = this.GetComponentInChildren<Text>();
            t.text = this.Channel;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            #if UNITY_6000_0_OR_NEWER
            ChatGui handler = FindFirstObjectByType<ChatGui>();
            #else
            ChatGui handler = FindObjectOfType<ChatGui>();
            #endif

            handler.ShowChannel(this.Channel);
        }
    }
}