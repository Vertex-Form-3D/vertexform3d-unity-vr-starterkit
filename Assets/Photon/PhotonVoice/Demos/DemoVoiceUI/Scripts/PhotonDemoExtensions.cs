namespace Photon.Voice.Unity.Demos.DemoVoiceUI
{
    using Realtime;
    using ExitGames.Client.Photon;

    public static partial class PhotonDemoExtensions // todo: USE C.A.S. ALWAYS
    {
        // this demo uses a Custom Property (as explained in the Realtime API), to sync if a player muted her microphone. that value needs a string key.
        internal const string IS_MUTED_PROPERTY_KEY = "mute";
        
        public static bool Mute(this Player player)
        {
            return player.SetCustomProperties(new Hashtable(1) { { IS_MUTED_PROPERTY_KEY, true } });
        }

        public static bool Unmute(this Player player)
        {
            return player.SetCustomProperties(new Hashtable(1) { { IS_MUTED_PROPERTY_KEY, false } });
        }

        public static bool IsMuted(this Player player)
        {
            object temp;
            return player.CustomProperties.TryGetValue(IS_MUTED_PROPERTY_KEY, out temp) && (bool)temp;
        }
    }
}