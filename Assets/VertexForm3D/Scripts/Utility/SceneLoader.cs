using Fusion;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

namespace VertexFormCore
{
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance;
        public bool isFlyModeEnabled;
        public float completePerchantage;
        public bool isCesiumScene;
        public bool sceneIsLoaded;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
#if !UNITY_WEBGL 
                StartCoroutine(EnsureXRInitializedAtStartup());
#endif
            }
            else
            {
                Destroy(gameObject);
            }
        }

#if !UNITY_WEBGL
        private IEnumerator EnsureXRInitializedAtStartup()
        {
            if (XRGeneralSettings.Instance == null || XRGeneralSettings.Instance.Manager == null)
                yield break;
            if (!XRGeneralSettings.Instance.Manager.isInitializationComplete)
            {
                Debug.Log("[SceneLoader] XR not initialized at startup — initializing now.");
                yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
                if (XRGeneralSettings.Instance.Manager.activeLoader != null)
                {
                    XRGeneralSettings.Instance.Manager.StartSubsystems();
                    Debug.Log("[SceneLoader] XR loader started: " + XRGeneralSettings.Instance.Manager.activeLoader.name);
                }
                else
                {
                    Debug.LogWarning("[SceneLoader] XR initialization ran but no active loader found — check XR Plug-in Management settings for this platform.");
                }
            }
        }
#endif

        Coroutine loadSceneCoroutine;
        public void LoadScnene(string SceneName)
        {
            sceneIsLoaded = false;
            if (loadSceneCoroutine == null)
            {
                loadSceneCoroutine = StartCoroutine(WaitToLeveThenLoadScene(SceneName));
            }
        }

        public IEnumerator WaitToLeveThenLoadScene(string SceneName)
        {
            // Check if we have a Fusion runner and if it's in a session
            bool isInSession = RoomManager.Instance != null &&
                              RoomManager.Instance.Runner != null &&
                              RoomManager.Instance.Runner.IsClient;

            if (isInSession)
            {
                // Leave the current Fusion session
                RoomManager.Instance.LeaveRoom();
            }

            // Wait for Fusion runner to shut down
            while (RoomManager.Instance != null &&
                   RoomManager.Instance.Runner != null &&
                   RoomManager.Instance.Runner.IsClient)
            {
                yield return new WaitForSeconds(1f);
            }

            Debug.Log($"[SceneLoader] Preparing to load addressable scene via Fusion: {SceneName}");
            completePerchantage = 0;

            // Load the base scene
            var baseSceneOp = SceneManager.LoadSceneAsync("addressableScene");
            while (!baseSceneOp.isDone)
            {
                completePerchantage = baseSceneOp.progress * 50f; // Base scene is 50% of loading
                yield return null;
            }

            Debug.Log("[SceneLoader] Base scene loaded, waiting for scene to be ready...");
            yield return new WaitForSeconds(0.5f);

#if !UNITY_WEBGL
            if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
            {
                if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
                {
                    XRGeneralSettings.Instance.Manager.StopSubsystems();
                    XRGeneralSettings.Instance.Manager.DeinitializeLoader();
                    Debug.Log("[SceneLoader] XR session stopped for scene transition.");
                }
                yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
                if (XRGeneralSettings.Instance.Manager.activeLoader != null)
                {
                    XRGeneralSettings.Instance.Manager.StartSubsystems();
                    Debug.Log("[SceneLoader] XR session started: " + XRGeneralSettings.Instance.Manager.activeLoader.name);
                }
                else
                {
                    Debug.LogWarning("[SceneLoader] XR loader not found after scene transition — check XR Plug-in Management settings.");
                }
            }
#endif

            // DON'T load the addressable scene here - let Fusion do it!
            // This ensures NetworkObjects in the addressable scene are properly networked
            // Fusion will load it via CustomNetworkSceneManager

            // Just call OnSceneLoaded with the scene name to trigger the Fusion connection
            // Fusion's CustomNetworkSceneManager will handle the actual addressable scene loading
            OnSceneLoaded(SceneName);

            loadSceneCoroutine = null;
        }

        string currentScene;

        public void OnSceneLoaded(string sceneName)
        {
            currentScene = sceneName;
            Debug.Log($"[SceneLoader] Base scene ready. Connecting to Fusion room: {sceneName}");

            // Connect to Fusion room
            // Fusion's CustomNetworkSceneManager will load the addressable scene for all clients
            // This ensures NetworkObjects in the addressable scene are properly networked
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.ConnectToRoom(sceneName);
            }
            else
            {
                Debug.LogError("[SceneLoader] RoomManager.Instance is null! Cannot connect to room.");
            }

#if !UNITY_WEBGL
            // WARNING: on Android with remote-hosted Addressables, Caching.ClearCache() can
            // invalidate the bundle Fusion is about to / just finished downloading and force a
            // re-download on the next visit. Only call this from an explicit "clear data"
            // action, not on every scene transition.
            // Caching.ClearCache();
#endif
        }

        public void OnFusionSceneLoaded(string sceneName)
        {
            // Called by RoomManager when Fusion has finished loading the addressable scene.
            // We do NOT flip sceneIsLoaded / completePerchantage here — both are set at the end
            // of FinalizeFusionSceneLoad, after the post-load main-thread stall is done. That
            // way the UI/progress bar can't hand control back to the user during the freeze
            // window that makes the VR world appear glued to the user's head.
            currentScene = sceneName;
            Debug.Log($"[SceneLoader] Fusion addressable scene fully loaded: {sceneName} — finalizing.");

            if (EventSystemHandler.Instance != null)
                EventSystemHandler.Instance.RemoveForeignEventSystems();

            StartCoroutine(FinalizeFusionSceneLoad(sceneName));
        }

        /// <summary>
        /// Runs the expensive post-load work without blocking the main thread for the full duration,
        /// which on Android/Quest with a freshly-downloaded remote bundle would otherwise present
        /// as "stuck at 100%" with VR head-locked frames (last frame stays on-screen while head
        /// poses keep updating, so the world appears to rotate with the user).
        /// </summary>
        private IEnumerator FinalizeFusionSceneLoad(string sceneName)
        {
            // 1) Activate the world scene so RenderSettings (skybox/ambient/fog) match it.
            //    Defer DynamicGI.UpdateEnvironment() by a frame so the first post-activation
            //    frame stays cheap — UpdateEnvironment is a known spike on Android GLES.
            if (TryResolveWorldScene(out var worldScene) && worldScene.isLoaded)
            {
                if (SceneManager.GetActiveScene() != worldScene)
                {
                    SceneManager.SetActiveScene(worldScene);
                    Debug.Log($"[SceneLoader] Active scene set to \"{worldScene.name}\" (path: {worldScene.path}).");
                }
            }
            else
            {
                Debug.LogWarning($"[SceneLoader] Finalize: could not resolve loaded world scene for key \"{sceneName}\".");
            }

            yield return null;
            DynamicGI.UpdateEnvironment();

            // 2) Let the renderer present a couple of frames so VR head tracking stays fluid
            //    before we start the unload pass.
            yield return null;
            yield return null;

            // 3) Async unload — Unity chunks the work across frames instead of one giant
            //    synchronous call. This is the single biggest fix for the "frozen frame"
            //    hitch after a remote bundle download.
            var unloadOp = Resources.UnloadUnusedAssets();
            while (unloadOp != null && !unloadOp.isDone)
                yield return null;

            // 4) Wait until the local VR player is spawned before dropping the curtain.
            //    Even after the unload is done, shaders may still be compiling and
            //    networked objects initializing — revealing the scene too soon causes a
            //    black/flickery frame burst in VR. Cap the wait so we don't wait forever
            //    if spawn never arrives (e.g. spectator mode, spawn error).
            float waitedForPlayer = 0f;
            const float playerSpawnTimeout = 15f;
            while (waitedForPlayer < playerSpawnTimeout)
            {
                if (RoomManager.Instance != null && RoomManager.Instance.localVRPlayer != null)
                    break;
                waitedForPlayer += Time.deltaTime;
                yield return null;
            }

            if (waitedForPlayer >= playerSpawnTimeout)
                Debug.LogWarning("[SceneLoader] Finalize: timed out waiting for local player spawn — dropping curtain anyway.");

            // 5) Let a few more frames render with the player present so shader warm-up
            //    and initial GI settle before the fade starts.
            for (int i = 0; i < 5; i++)
                yield return null;

            // 6) Only now is it safe to tell the rest of the app we're done. Anything gating
            //    on sceneIsLoaded / completePerchantage == 100 (curtain fade, input handoff,
            //    UI dismiss) will see a stable, presenting renderer.
            sceneIsLoaded = true;
            completePerchantage = 100f;

            Debug.Log($"[SceneLoader] Finalize complete for: {sceneName}");
        }

        /// <summary>
        /// Sets the active scene to the loaded addressable world so global RenderSettings
        /// (skybox, ambient, fog) match that scene. Fusion loads the world additively while
        /// the base scene may stay active otherwise.
        /// </summary>
        [ContextMenu("ActivateScene")]
        public void ActivateScene()
        {
            if (!TryResolveWorldScene(out var worldScene))
            {
                Debug.LogWarning($"[SceneLoader] ActivateScene: could not resolve loaded scene for key \"{currentScene}\".");
                return;
            }

            if (!worldScene.isLoaded)
            {
                Debug.LogWarning($"[SceneLoader] ActivateScene: scene \"{worldScene.name}\" is not loaded yet.");
                return;
            }

            if (SceneManager.GetActiveScene() == worldScene)
                return;

            SceneManager.SetActiveScene(worldScene);
            DynamicGI.UpdateEnvironment();
            Debug.Log($"[SceneLoader] Active scene set to \"{worldScene.name}\" (path: {worldScene.path}).");
        }

        public bool TryResolveWorldScene(out Scene scene)
        {
            scene = default;
            if (string.IsNullOrEmpty(currentScene))
                return false;

            string key = currentScene.Replace('\\', '/');

            scene = SceneManager.GetSceneByName(currentScene);
            if (scene.IsValid() && scene.isLoaded)
                return true;

            string shortName = Path.GetFileNameWithoutExtension(key);
            if (!string.IsNullOrEmpty(shortName) && shortName != currentScene)
            {
                scene = SceneManager.GetSceneByName(shortName);
                if (scene.IsValid() && scene.isLoaded)
                    return true;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!s.IsValid() || !s.isLoaded)
                    continue;
                if (s.name == "addressableScene" || s.name == "DontDestroyOnLoad")
                    continue;
                if (!string.IsNullOrEmpty(s.path))
                {
                    string sp = s.path.Replace('\\', '/');
                    if (sp == key || sp.EndsWith(key, StringComparison.OrdinalIgnoreCase))
                    {
                        scene = s;
                        return true;
                    }
                }
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!s.IsValid() || !s.isLoaded)
                    continue;
                if (s.name == "addressableScene" || s.name == "DontDestroyOnLoad")
                    continue;
                if (!string.IsNullOrEmpty(shortName) && s.name == shortName)
                {
                    scene = s;
                    return true;
                }
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!s.IsValid() || !s.isLoaded)
                    continue;
                if (s.name == "addressableScene" || s.name == "DontDestroyOnLoad")
                    continue;
                scene = s;
                return true;
            }

            return false;
        }
    }
}