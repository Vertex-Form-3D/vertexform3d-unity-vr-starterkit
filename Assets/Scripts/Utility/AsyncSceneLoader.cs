using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AsyncSceneLoader 
{
    public static System.Collections.IEnumerator LoadSceneAsyncCoroutine(string sceneToLoad)
    {
        // Show a loading screen or do any necessary preparations

        // Asynchronously load the scene
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneToLoad);

        // Wait until the asynchronous operation is complete
        while (!asyncOperation.isDone)
        {
            // Optionally, you can use asyncOperation.progress to get the loading progress
            float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
            //   Debug.Log($"Loading progress: {progress * 100}%");

            yield return null;
        }

        // The scene has finished loading
        Debug.Log($"Scene '{sceneToLoad}' has been loaded.");

        // Optionally, you can perform any post-loading tasks here

        Resources.UnloadUnusedAssets();
    }
}
