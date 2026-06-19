using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

namespace QuantumVertex
{
    /// <summary>
    /// Redirects a VideoPlayer to a StreamingAssets URL at startup, replacing an
    /// embedded clip. Embedded VideoClips are unsupported on web builds.
    ///
    /// On WebGL, downloads the file and plays it from a browser blob URL so playback
    /// works even when the host does not support HTTP byte-range requests (Unity's
    /// localhost dev server, some static hosts). Other platforms use the direct URL.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    [DefaultExecutionOrder(-100)]
    public class StreamingVideoSource : MonoBehaviour
    {
        [Tooltip("File name inside Assets/StreamingAssets, e.g. LoginBackgroundVideo.mp4")]
        [SerializeField] private string fileName = "LoginBackgroundVideo.mp4";

        private VideoPlayer player;
        private string blobUrl;
        private bool urlReady;

        public bool UrlReady => urlReady;

        public IEnumerator WaitUntilReady()
        {
            while (!urlReady)
                yield return null;
        }

        private void Awake()
        {
            player = GetComponent<VideoPlayer>();
            player.clip = null;
            player.source = VideoSource.Url;
            player.url = string.Empty;

#if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(ConfigureUrl());
#else
            var streamingUrl = BuildStreamingAssetsUrl(fileName);
            player.url = streamingUrl;
            urlReady = true;
            Debug.Log($"[StreamingVideoSource] streamingAssetsPath={Application.streamingAssetsPath} url={streamingUrl}");
#endif
        }

        private IEnumerator ConfigureUrl()
        {
            var streamingUrl = BuildStreamingAssetsUrl(fileName);
            Debug.Log($"[StreamingVideoSource] streamingAssetsPath={Application.streamingAssetsPath} url={streamingUrl}");

#if UNITY_WEBGL && !UNITY_EDITOR
            yield return LoadWebGlVideoUrl(streamingUrl);
#else
            player.url = streamingUrl;
            urlReady = true;
            yield break;
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator LoadWebGlVideoUrl(string streamingUrl)
        {
            using var request = UnityWebRequest.Get(streamingUrl);
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"[StreamingVideoSource] Failed to download '{streamingUrl}' ({request.responseCode}): {request.error}. " +
                    "Rebuild the WebGL player after placing the file in Assets/StreamingAssets/, then open that URL in the browser to confirm it returns 200.",
                    this);
                player.url = streamingUrl;
                urlReady = true;
                yield break;
            }

            var bytes = request.downloadHandler.data;
            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogError($"[StreamingVideoSource] Downloaded video is empty: '{streamingUrl}'", this);
                player.url = streamingUrl;
                urlReady = true;
                yield break;
            }

            blobUrl = CreateBlobUrl(bytes, "video/mp4");
            if (string.IsNullOrEmpty(blobUrl))
            {
                Debug.LogWarning("[StreamingVideoSource] Blob URL creation failed; falling back to direct HTTP URL.", this);
                player.url = streamingUrl;
            }
            else
            {
                player.url = blobUrl;
                Debug.Log($"[StreamingVideoSource] WebGL blob URL ready ({bytes.Length} bytes downloaded).");
            }

            urlReady = true;
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(blobUrl))
                RevokeBlobUrl(blobUrl);
        }

        [DllImport("__Internal")]
        private static extern IntPtr VF3D_CreateBlobUrlFromBuffer(IntPtr bufferPtr, int length, string mimeType);

        [DllImport("__Internal")]
        private static extern void VF3D_RevokeBlobUrl(string url);

        static string CreateBlobUrl(byte[] data, string mimeType)
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var ptr = VF3D_CreateBlobUrlFromBuffer(handle.AddrOfPinnedObject(), data.Length, mimeType);
                if (ptr == IntPtr.Zero)
                    return null;
                return Marshal.PtrToStringUTF8(ptr);
            }
            finally
            {
                handle.Free();
            }
        }

        static void RevokeBlobUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;
            VF3D_RevokeBlobUrl(url);
        }
#endif

        internal static string BuildStreamingAssetsUrl(string fileName)
        {
            var basePath = Application.streamingAssetsPath;
            if (string.IsNullOrEmpty(basePath))
                return fileName;

            return $"{basePath.TrimEnd('/', '\\')}/{fileName}";
        }
    }
}
