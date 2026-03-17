using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace VertexFormCore
{
    /// <summary>
    /// Custom NetworkSceneManager that properly resolves addressable scene paths
    /// for cross-platform multiplayer with addressable content.
    /// 
    /// Usage:
    /// 1. Before connecting to Fusion, register your addressable scenes using RegisterAddressableScene()
    /// 2. Fusion will use these registered scenes when loading via SceneRef.FromPath()
    /// 3. This ensures cross-platform compatibility without relying on build indices
    /// </summary>
    public class CustomNetworkSceneManager : NetworkSceneManagerDefault
    {
        // Cache of addressable scene keys/addresses that we know about
        private static HashSet<string> knownAddressableScenes = new HashSet<string>();

        public override void Initialize(NetworkRunner runner)
        {
            base.Initialize(runner);
            Debug.Log($"[CustomNetworkSceneManager] Initialized for runner: {runner.name}. Registered scenes: {knownAddressableScenes.Count}");
            if (knownAddressableScenes.Count > 0)
            {
                Debug.Log($"[CustomNetworkSceneManager] Registered scenes: {string.Join(", ", knownAddressableScenes)}");
            }
        }

        public static void RegisterAddressableScene(string sceneKey)
        {
            if (!string.IsNullOrEmpty(sceneKey))
            {
                knownAddressableScenes.Add(sceneKey);
                Debug.Log($"[CustomNetworkSceneManager] Registered addressable scene: '{sceneKey}'. Total registered: {knownAddressableScenes.Count}");
            }
        }

        public static void ClearRegisteredScenes()
        {
            knownAddressableScenes.Clear();
            Debug.Log($"[CustomNetworkSceneManager] Cleared all registered scenes");
        }

        public static bool IsSceneRegistered(string sceneKey)
        {
            return knownAddressableScenes.Contains(sceneKey);
        }

        /// <summary>
        /// Override to provide custom addressable scene resolution.
        /// This avoids the need to wait for Fusion to query the addressables catalog.
        /// </summary>
        protected override GetAddressableScenesResult GetAddressableScenes()
        {
            Debug.Log($"[CustomNetworkSceneManager] GetAddressableScenes called");

            // First try to get scenes with the FusionScenes label (default behavior)
            var defaultResult = base.GetAddressableScenes();

            // Create a task that combines both the default result and our known scenes
            var tcs = new TaskCompletionSource<string[]>();

            defaultResult.Task.ContinueWith(task =>
            {
                try
                {
                    if (task.IsCompletedSuccessfully && task.Result != null && task.Result.Length > 0)
                    {
                        // Use the scenes from the catalog
                        var catalogScenes = task.Result.ToList();

                        // Add any registered scenes that aren't already in the list
                        foreach (var knownScene in knownAddressableScenes)
                        {
                            if (!catalogScenes.Contains(knownScene))
                            {
                                catalogScenes.Add(knownScene);
                            }
                        }

                        Debug.Log($"[CustomNetworkSceneManager] Found {catalogScenes.Count} addressable scenes: {string.Join(", ", catalogScenes)}");
                        tcs.SetResult(catalogScenes.ToArray());
                    }
                    else
                    {
                        // If catalog query failed or returned nothing, use only our known scenes
                        Debug.LogWarning($"[CustomNetworkSceneManager] Catalog query failed or empty, using registered scenes only");
                        tcs.SetResult(knownAddressableScenes.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CustomNetworkSceneManager] Error resolving addressable scenes: {ex.Message}");
                    // Fallback to known scenes
                    tcs.SetResult(knownAddressableScenes.ToArray());
                }
            });

            return new GetAddressableScenesResult
            {
                Task = tcs.Task,
                BeforeWaitForCompletion = () =>
                {
                    // Call the default before wait handler
                    defaultResult.BeforeWaitForCompletion?.Invoke();
                }
            };
        }
    }
}
