using Fusion;
using System.Collections;
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
            }
            else
            {
                Destroy(gameObject);
            }
        }

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
            if (XRGeneralSettings.Instance != null &&
                XRGeneralSettings.Instance.Manager != null &&
                XRGeneralSettings.Instance.Manager.isInitializationComplete)
            {
                XRGeneralSettings.Instance.Manager.StopSubsystems();
                XRGeneralSettings.Instance.Manager.DeinitializeLoader();
                Debug.Log("XR session stopped.");
                yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
                if (XRGeneralSettings.Instance.Manager.activeLoader != null)
                {
                    XRGeneralSettings.Instance.Manager.StartSubsystems();
                    Debug.Log("XR session reinitialized.");
                }
                else
                {
                    Debug.LogError("Failed to reinitialize XR Loader.");
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
        
        [ContextMenu("ActivateScene")]
        public void ActivateScene()
        {
            Scene sc = SceneManager.GetSceneByName(currentScene);
            if (sc.IsValid())
            {
                SceneManager.SetActiveScene(sc);
            }
        }
        
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

            Resources.UnloadUnusedAssets();
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
        }
    }
}