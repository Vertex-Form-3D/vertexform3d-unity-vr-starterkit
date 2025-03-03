using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace VertextFormCore
{
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance;
        public bool isFlyModeEnabled;
        public float completePerchantage;
        public bool isCesiumScene;
        public CesiumWorldClass cesiumWorldClass = new CesiumWorldClass();

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
            SceneManager.LoadSceneAsync("addressableScene");

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
            Debug.Log(" scene loaded " + sceneName);
            RoomManager.Instance.ConnectToRoom(sceneName);

            Scene sc = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(sc);
            Resources.UnloadUnusedAssets();
            Caching.ClearCache();
        }
    }
}