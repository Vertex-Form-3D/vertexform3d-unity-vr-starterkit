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

            Debug.Log("addressable SceneName is : " + SceneName);
            completePerchantage = 0;
            SceneManager.LoadSceneAsync(2);

            AsyncOperationHandle<SceneInstance> sceneHandle = Addressables.LoadSceneAsync(SceneName, LoadSceneMode.Additive, true);
            sceneHandle.Completed += (x) =>
            {
                OnSceneLoaded(SceneName);
            };

            if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
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

            Debug.Log("LoadSceneAsync: " + SceneName);
            while (!sceneHandle.IsDone)
            {
                completePerchantage = sceneHandle.PercentComplete * 100f;
                Debug.Log("Scene is not done yet please wait");
                yield return new WaitForSeconds(1f);
            }
            yield return sceneHandle;


            if (sceneHandle.Status == AsyncOperationStatus.Succeeded)
            {
                completePerchantage = sceneHandle.PercentComplete * 100f;

                yield return sceneHandle.Result.ActivateAsync();
                Debug.Log("operation successful");
            }
            else
            {
                Debug.LogError("operation failed due to " + sceneHandle.OperationException);
                if (VirtualRoomManager.Instance != null)
                {
                    VirtualRoomManager.Instance.LeaveRoomAndLoadHomeScene();
                }
                AssetBundle.UnloadAllAssetBundles(false);
                Resources.UnloadUnusedAssets();
            }
            loadSceneCoroutine = null;
        }

        string currentScene;
        [ContextMenu("ActivateScene")]
        public void ActivateScene()
        {
            Scene sc = SceneManager.GetSceneByName(currentScene);
            SceneManager.SetActiveScene(sc);
        }
        public void OnSceneLoaded(string sceneName)
        {
            sceneIsLoaded = true;
            currentScene = sceneName;
            Debug.Log(" scene loaded " + sceneName);

            // Connect to Fusion room using the new scene name
            // Note: Fusion will handle setting the active scene when it loads the networked version
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.ConnectToRoom(sceneName);
            }

            // Don't set active scene here - Fusion's NetworkSceneManager will handle it
            // when it loads the scene for networking. Setting it here causes duplicate activation.

            Resources.UnloadUnusedAssets();
            Caching.ClearCache();
        }
    }
}