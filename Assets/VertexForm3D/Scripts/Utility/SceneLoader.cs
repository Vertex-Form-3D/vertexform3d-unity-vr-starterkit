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
            Caching.ClearCache();
#endif
        }

        public void OnFusionSceneLoaded(string sceneName)
        {
            // Called by RoomManager when Fusion has finished loading the addressable scene
            sceneIsLoaded = true;
            completePerchantage = 100f;
            currentScene = sceneName;
            Debug.Log($"[SceneLoader] Fusion addressable scene fully loaded: {sceneName}");

            // Apply world lighting/skybox before unloading: RenderSettings follow the active scene.
            ActivateScene();

            // Defer cleanup until the additive scene is present; early UnloadUnusedAssets during
            // async load has caused missing skybox/textures on first WebGL visit.
            Resources.UnloadUnusedAssets();
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

        bool TryResolveWorldScene(out Scene scene)
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