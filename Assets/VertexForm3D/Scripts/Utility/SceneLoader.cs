using Photon.Pun;
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
        public CesiumWorldClass cesiumWorldClass = new CesiumWorldClass();
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
            PhotonNetwork.GameVersion = Application.version;
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
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }

            while (PhotonNetwork.InRoom)
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
            Debug.Log("LoadSceneAsync: " + SceneName);
            while (!sceneHandle.IsDone)
            {
                completePerchantage = sceneHandle.PercentComplete * 100f;
                Debug.Log("Scene is not done yet please wait");
                yield return new WaitForSeconds(1f);
            }
            yield return sceneHandle;

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


        public void OnSceneLoaded(string sceneName)
        {
            sceneIsLoaded = true;
            Debug.Log(" scene loaded " + sceneName);
            RoomManager.Instance.ConnectToRoom(sceneName);

            Scene sc = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(sc);
            Resources.UnloadUnusedAssets();
            Caching.ClearCache();
        }
    }
}