using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

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
        /// Merges the Fusion catalog (FusionScenes label) with <see cref="knownAddressableScenes"/>.
        /// </summary>
        /// <remarks>
        /// The base implementation completes its task in <see cref="GetAddressableScenesResult.BeforeWaitForCompletion"/>.
        /// We must merge catalog paths with registered keys in that same callback — not in <c>ContinueWith</c> —
        /// or WebGL can deadlock: the main thread blocks in <c>Task.Wait</c> while continuations never run.
        /// </remarks>
        protected override GetAddressableScenesResult GetAddressableScenes()
        {
            Debug.Log("[CustomNetworkSceneManager] GetAddressableScenes called");

            var defaultResult = base.GetAddressableScenes();
            var tcs = new TaskCompletionSource<string[]>();

            return new GetAddressableScenesResult
            {
                Task = tcs.Task,
                BeforeWaitForCompletion = () =>
                {
                    defaultResult.BeforeWaitForCompletion?.Invoke();

                    string[] catalogPaths = Array.Empty<string>();
                    try
                    {
                        if (defaultResult.Task.Status == TaskStatus.RanToCompletion)
                            catalogPaths = defaultResult.Task.Result ?? Array.Empty<string>();
                        else if (defaultResult.Task.IsFaulted)
                            Debug.LogWarning($"[CustomNetworkSceneManager] Default addressables query failed: {defaultResult.Task.Exception?.GetBaseException()?.Message}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[CustomNetworkSceneManager] Reading catalog paths failed: {ex.Message}");
                    }

                    var merged = new List<string>(catalogPaths.Length + knownAddressableScenes.Count);
                    foreach (var p in catalogPaths)
                    {
                        if (!string.IsNullOrEmpty(p) && !merged.Contains(p))
                            merged.Add(p);
                    }

                    foreach (var known in knownAddressableScenes)
                    {
                        if (!string.IsNullOrEmpty(known) && !merged.Contains(known))
                            merged.Add(known);
                    }

                    Debug.Log($"[CustomNetworkSceneManager] Merged {merged.Count} addressable scene path(s) for Fusion: {string.Join(", ", merged)}");
                    tcs.TrySetResult(merged.ToArray());
                }
            };
        }
    }
}
