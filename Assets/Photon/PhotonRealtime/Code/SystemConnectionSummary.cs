// ----------------------------------------------------------------------------
// <copyright file="ConnectionHandler.cs" company="Exit Games GmbH">
//   Loadbalancing Framework for Photon - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
//   If the game logic does not call Service() for whatever reason, this keeps the connection.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------


#if UNITY_4_7 || UNITY_5 || UNITY_5_3_OR_NEWER
#define SUPPORTED_UNITY
#endif



namespace Photon.Realtime
{
    using System.Text;


    /// <summary>
    /// The SystemConnectionSummary (SBS) is useful to analyze low level connection issues in Unity. This requires a ConnectionHandler in the scene.
    /// </summary>
    /// <remarks>
    /// A LoadBalancingClient automatically creates a SystemConnectionSummary on these disconnect causes:
    /// DisconnectCause.ExceptionOnConnect, DisconnectCause.Exception, DisconnectCause.ServerTimeout and DisconnectCause.ClientTimeout.
    ///
    /// The SBS can then be turned into an integer (ToInt()) or string to debug the situation or use in analytics.
    /// Both, ToString and ToInt summarize the network-relevant conditions of the client at and before the connection fail, including the PhotonPeer.SocketErrorCode.
    ///
    /// Important: To correctly create the SBS instance, a ConnectionHandler component must be present and enabled in the
    /// Unity scene hierarchy. In best case, keep the ConnectionHandler on a GameObject which is flagged as
    /// DontDestroyOnLoad.
    /// </remarks>
    public class SystemConnectionSummary
    {
        // SystemConditionSummary v0  has 32 bits:
        // Version bits (4 bits)
        // UDP, TCP, WS, WSS (WebRTC potentially) (3 bits)
        // 1 bit empty
        //
        // AppQuits
        // AppPause
        // AppPauseRecent
        // AppOutOfFocus
        //
        // AppOutOfFocusRecent
        // NetworkReachability (Unity value)
        // ErrorCodeFits (ErrorCode > short.Max would be a problem)
        // WinSock (true) or BSD (false) Socket Error Codes
        //
        // Time since receive?
        // Times of send?!
        //
        // System/Platform -> should be in other analytic values (not this)

        public readonly byte Version = 0;

        public byte UsedProtocol;

        public bool AppQuits;
        public bool AppPause;
        public bool AppPauseRecent;
        public bool AppOutOfFocus;

        public bool AppOutOfFocusRecent;
        public bool NetworkReachable;
        public bool ErrorCodeFits;
        public bool ErrorCodeWinSock;

        public int SocketErrorCode;

        private static readonly string[] ProtocolIdToName = { "UDP", "TCP", "2(N/A)", "3(N/A)", "WS", "WSS", "6(N/A)", "7WebRTC" };

        private class SCSBitPos
        {
            /// <summary>28 and up. 4 bits.</summary>
            public const int Version = 28;
            /// <summary>25 and up. 3 bits.</summary>
            public const int UsedProtocol = 25;
            public const int EmptyBit = 24;

            public const int AppQuits = 23;
            public const int AppPause = 22;
            public const int AppPauseRecent = 21;
            public const int AppOutOfFocus = 20;

            public const int AppOutOfFocusRecent = 19;
            public const int NetworkReachable = 18;
            public const int ErrorCodeFits = 17;
            public const int ErrorCodeWinSock = 16;
        }


        /// <summary>
        /// Creates a SystemConnectionSummary for an incident of a local LoadBalancingClient. This gets used automatically by the LoadBalancingClient!
        /// </summary>
        /// <remarks>
        /// If the LoadBalancingClient.SystemConnectionSummary is non-null after a connection-loss, you can call .ToInt() and send this to analytics or log it.
        ///
        /// </remarks>
        /// <param name="client"></param>
        public SystemConnectionSummary(LoadBalancingClient client)
        {
            if (client != null)
            {
                // protocol = 3 bits! potentially adding WebRTC.
                this.UsedProtocol = (byte)((int)client.LoadBalancingPeer.UsedProtocol & 7);
                this.SocketErrorCode = (int)client.LoadBalancingPeer.SocketErrorCode;
            }

            this.AppQuits = ConnectionHandler.AppQuits;
            this.AppPause = ConnectionHandler.AppPause;
            this.AppPauseRecent = ConnectionHandler.AppPauseRecent;
            this.AppOutOfFocus = ConnectionHandler.AppOutOfFocus;

            this.AppOutOfFocusRecent = ConnectionHandler.AppOutOfFocusRecent;
            this.NetworkReachable = ConnectionHandler.IsNetworkReachableUnity();

            this.ErrorCodeFits = this.SocketErrorCode <= short.MaxValue; // socket error code <= short.Max (everything else is a problem)
            this.ErrorCodeWinSock = true;
        }

        /// <summary>
        /// Creates a SystemConnectionSummary instance from an int (reversing ToInt()). This can then be turned into a string again.
        /// </summary>
        /// <param name="summary">An int, as provided by ToInt(). No error checks yet.</param>
        public SystemConnectionSummary(int summary)
        {
            this.Version = GetBits(ref summary, SCSBitPos.Version, 0xF);
            this.UsedProtocol = GetBits(ref summary, SCSBitPos.UsedProtocol, 0x7);
            // 1 empty bit

            this.AppQuits = GetBit(ref summary, SCSBitPos.AppQuits);
            this.AppPause = GetBit(ref summary, SCSBitPos.AppPause);
            this.AppPauseRecent = GetBit(ref summary, SCSBitPos.AppPauseRecent);
            this.AppOutOfFocus = GetBit(ref summary, SCSBitPos.AppOutOfFocus);

            this.AppOutOfFocusRecent = GetBit(ref summary, SCSBitPos.AppOutOfFocusRecent);
            this.NetworkReachable = GetBit(ref summary, SCSBitPos.NetworkReachable);
            this.ErrorCodeFits = GetBit(ref summary, SCSBitPos.ErrorCodeFits);
            this.ErrorCodeWinSock = GetBit(ref summary, SCSBitPos.ErrorCodeWinSock);

            this.SocketErrorCode = summary & 0xFFFF;
        }

        /// <summary>
        /// Turns the SystemConnectionSummary into an integer, which can be be used for analytics purposes. It contains a lot of info and can be used to instantiate a new SystemConnectionSummary.
        /// </summary>
        /// <returns>Compact representation of the context for a disconnect issue.</returns>
        public int ToInt()
        {
            int result = 0;
            SetBits(ref result, this.Version, SCSBitPos.Version);
            SetBits(ref result, this.UsedProtocol, SCSBitPos.UsedProtocol);
            // 1 empty bit

            SetBit(ref result, this.AppQuits, SCSBitPos.AppQuits);
            SetBit(ref result, this.AppPause, SCSBitPos.AppPause);
            SetBit(ref result, this.AppPauseRecent, SCSBitPos.AppPauseRecent);
            SetBit(ref result, this.AppOutOfFocus, SCSBitPos.AppOutOfFocus);

            SetBit(ref result, this.AppOutOfFocusRecent, SCSBitPos.AppOutOfFocusRecent);
            SetBit(ref result, this.NetworkReachable, SCSBitPos.NetworkReachable);
            SetBit(ref result, this.ErrorCodeFits, SCSBitPos.ErrorCodeFits);
            SetBit(ref result, this.ErrorCodeWinSock, SCSBitPos.ErrorCodeWinSock);


            // insert socket error code as lower 2 bytes
            int socketErrorCode = this.SocketErrorCode & 0xFFFF;
            result |= socketErrorCode;

            return result;
        }

        /// <summary>
        /// A readable debug log string of the context for network problems.
        /// </summary>
        /// <returns>SystemConnectionSummary as readable string.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string transportProtocol = ProtocolIdToName[this.UsedProtocol];

            sb.Append($"SCS v{this.Version} {transportProtocol} SocketErrorCode: {this.SocketErrorCode} ");

            if (this.AppQuits) sb.Append("AppQuits ");
            if (this.AppPause) sb.Append("AppPause ");
            if (!this.AppPause && this.AppPauseRecent) sb.Append("AppPauseRecent ");
            if (this.AppOutOfFocus) sb.Append("AppOutOfFocus ");
            if (!this.AppOutOfFocus && this.AppOutOfFocusRecent) sb.Append("AppOutOfFocusRecent ");
            if (!this.NetworkReachable) sb.Append("NetworkUnreachable ");
            if (!this.ErrorCodeFits) sb.Append("ErrorCodeRangeExceeded ");

            if (this.ErrorCodeWinSock) sb.Append("WinSock");
            else sb.Append("BSDSock");

            string result = sb.ToString();
            return result;
        }


        public static bool GetBit(ref int value, int bitpos)
        {
            int result = (value >> bitpos) & 1;
            return result != 0;
        }

        public static byte GetBits(ref int value, int bitpos, byte mask)
        {
            int result = (value >> bitpos) & mask;
            return (byte)result;
        }

        /// <summary>Applies bitval to bitpos (no matter value's initial bit value).</summary>
        public static void SetBit(ref int value, bool bitval, int bitpos)
        {
            if (bitval)
            {
                value |= 1 << bitpos;
            }
            else
            {
                value &= ~(1 << bitpos);
            }
        }

        /// <summary>Applies bitvals via OR operation (expects bits in value to be 0 initially).</summary>
        public static void SetBits(ref int value, byte bitvals, int bitpos)
        {
            value |= bitvals << bitpos;
        }
    }
}