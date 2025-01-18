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
    using System;
    using System.Diagnostics;
    using SupportClass = ExitGames.Client.Photon.SupportClass;

    #if SUPPORTED_UNITY
    using UnityEngine;
    #endif


    #if SUPPORTED_UNITY
    public class ConnectionHandler : MonoBehaviour
    #else
    public class ConnectionHandler
    #endif
    {
        /// <summary>
        /// Photon client to log information and statistics from.
        /// </summary>
        public LoadBalancingClient Client { get; set; }

        /// <summary>Option to let the fallback thread call Disconnect after the KeepAliveInBackground time. Default: false.</summary>
        /// <remarks>
        /// If set to true, the thread will disconnect the client regularly, should the client not call SendOutgoingCommands / Service.
        /// This may happen due to an app being in background (and not getting a lot of CPU time) or when loading assets.
        ///
        /// If false, a regular timeout time will have to pass (on top) to time out the client.
        /// </remarks>
        public bool DisconnectAfterKeepAlive = false;

        /// <summary>Defines for how long the Fallback Thread should keep the connection, before it may time out as usual.</summary>
        /// <remarks>We want to the Client to keep it's connection when an app is in the background (and doesn't call Update / Service Clients should not keep their connection indefinitely in the background, so after some milliseconds, the Fallback Thread should stop keeping it up.</remarks>
        public int KeepAliveInBackground = 60000;

        /// <summary>Counts how often the Fallback Thread called SendAcksOnly, which is purely of interest to monitor if the game logic called SendOutgoingCommands as intended.</summary>
        public int CountSendAcksOnly { get; private set; }

        /// <summary>True if a fallback thread is running. Will call the client's SendAcksOnly() method to keep the connection up.</summary>
        public bool FallbackThreadRunning
        {
            get { return this.fallbackThreadId < 255; }
        }

        /// <summary>Keeps the ConnectionHandler, even if a new scene gets loaded.</summary>
        public bool ApplyDontDestroyOnLoad = true;

        /// <summary>Indicates that the app is closing. Set in OnApplicationQuit().</summary>
        [NonSerialized]
        public static bool AppQuits;
        [NonSerialized]
        public static bool AppPause;
        [NonSerialized]
        public static bool AppPauseRecent;
        [NonSerialized]
        public static bool AppOutOfFocus;
        [NonSerialized]
        public static bool AppOutOfFocusRecent;


        private byte fallbackThreadId = 255;
        private bool didSendAcks;
        private readonly Stopwatch backgroundStopwatch = new Stopwatch();


        #if SUPPORTED_UNITY

        #if UNITY_2019_4_OR_NEWER

        /// <summary>
        /// Resets statics for Domain Reload
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void StaticReset()
        {
            AppQuits = false;
            AppPause = false;
            AppPauseRecent = false;
            AppOutOfFocus = false;
            AppOutOfFocusRecent = false;
        }

        #endif


        /// <summary></summary>
        protected virtual void Awake()
        {
            if (this.ApplyDontDestroyOnLoad)
            {
                DontDestroyOnLoad(this.gameObject);
            }
        }

        /// <summary>Called by Unity when the application gets closed. Disconnects if OnApplicationQuit() was called before.</summary>
        protected virtual void OnDisable()
        {
            this.StopFallbackSendAckThread();

            if (AppQuits)
            {
                if (this.Client != null && this.Client.IsConnected)
                {
                    this.Client.Disconnect(DisconnectCause.ApplicationQuit);
                    this.Client.LoadBalancingPeer.StopThread();
                }

                SupportClass.StopAllBackgroundCalls();
            }
        }


        /// <summary>Called by Unity when the application gets closed. The UnityEngine will also call OnDisable, which disconnects.</summary>
        public void OnApplicationQuit()
        {
            AppQuits = true;
        }

        /// <summary>Called by Unity when the application gets paused or resumed.</summary>
        public void OnApplicationPause(bool pause)
        {
            AppPause = pause;

            if (pause)
            {
                AppPauseRecent = true;
                this.CancelInvoke(nameof(this.ResetAppPauseRecent));
            }
            else
            {
                Invoke(nameof(this.ResetAppPauseRecent), 5f);
            }
        }

        private void ResetAppPauseRecent()
        {
            AppPauseRecent = false;
        }

        /// <summary>Called by Unity when the application changes focus.</summary>
        public void OnApplicationFocus(bool focus)
        {
            AppOutOfFocus = !focus;
            if (!focus)
            {
                AppOutOfFocusRecent = true;
                this.CancelInvoke(nameof(this.ResetAppOutOfFocusRecent));
            }
            else
            {
                this.Invoke(nameof(this.ResetAppOutOfFocusRecent), 5f);
            }
        }

        private void ResetAppOutOfFocusRecent()
        {
            AppOutOfFocusRecent = false;
        }


        #endif


        /// <summary>
        /// When run in Unity, this returns Application.internetReachability != NetworkReachability.NotReachable.
        /// </summary>
        /// <returns>Application.internetReachability != NetworkReachability.NotReachable</returns>
        public static bool IsNetworkReachableUnity()
        {
            #if SUPPORTED_UNITY
            return Application.internetReachability != NetworkReachability.NotReachable;
            #else
            return true;
            #endif
        }

        public void StartFallbackSendAckThread()
        {
            #if !UNITY_WEBGL
            if (this.FallbackThreadRunning)
            {
                return;
            }

            #if UNITY_SWITCH
            this.fallbackThreadId = SupportClass.StartBackgroundCalls(this.RealtimeFallbackThread, 50);  // as workaround, we don't name the Thread.
            #else
            this.fallbackThreadId = SupportClass.StartBackgroundCalls(this.RealtimeFallbackThread, 50, "RealtimeFallbackThread");
            #endif
            #endif
        }

        public void StopFallbackSendAckThread()
        {
            #if !UNITY_WEBGL
            if (!this.FallbackThreadRunning)
            {
                return;
            }

            SupportClass.StopBackgroundCalls(this.fallbackThreadId);
            this.fallbackThreadId = 255;
            #endif
        }


        /// <summary>A thread which runs independent from the Update() calls. Keeps connections online while loading or in background. See <see cref="KeepAliveInBackground"/>.</summary>
        public bool RealtimeFallbackThread()
        {
            if (this.Client != null)
            {
                if (!this.Client.IsConnected)
                {
                    this.didSendAcks = false;
                    return true;
                }

                if (this.Client.LoadBalancingPeer.ConnectionTime - this.Client.LoadBalancingPeer.LastSendOutgoingTime > 100)
                {
                    if (!this.didSendAcks)
                    {
                        backgroundStopwatch.Reset();
                        backgroundStopwatch.Start();
                    }

                    // check if the client should disconnect after some seconds in background
                    if (backgroundStopwatch.ElapsedMilliseconds > this.KeepAliveInBackground)
                    {
                        if (this.DisconnectAfterKeepAlive)
                        {
                            this.Client.Disconnect();
                        }
                        return true;
                    }


                    this.didSendAcks = true;
                    this.CountSendAcksOnly++;
                    this.Client.LoadBalancingPeer.SendAcksOnly();
                }
                else
                {
                    this.didSendAcks = false;
                }
            }

            return true;
        }
    }
}