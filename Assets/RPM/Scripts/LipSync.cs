using System;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace ReadyPlayerMe.Core
{
    /// <summary>
    /// This enumeration describes the options for AudioProviderType.
    /// </summary>
    public enum AudioProviderType
    {
        Microphone = 0,
        AudioClip = 1
    }

    /// <summary>
    /// This class is responsible for adding basic facial animations at runtime using microphone input and facial blendshape
    /// manipulation
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Ready Player Me/LipSync", 0)]
    public class LipSync : MonoBehaviour
    {
        private const string MOUTH_OPEN_BLEND_SHAPE_NAME = "mouthOpen";
        private const int AMPLITUDE_MULTIPLIER = 10;
        private const int AUDIO_SAMPLE_LENGTH = 4096;
        private const int MICROPHONE_FREQUENCY = 44100;
        private const string MISSING_BLENDSHAPE_MESSAGE = "The 'mouthOpen' morph target is required for LipSync.cs but it was not found on Avatar mesh. Use an AvatarConfig to specify the blendshapes to be included on loaded avatars.";
        private const string MICROPHONE_IS_NOT_SUPPORTED_IN_WEBGL = "Microphone is not supported in WebGL.";

        private float[] audioSample = new float[AUDIO_SAMPLE_LENGTH];

        public AudioClip AudioClip;
        public AudioSource AudioSource;
        public AudioProviderType AudioProvider = AudioProviderType.Microphone;
        private Dictionary<SkinnedMeshRenderer, int> blendshapeMeshIndexMap;
        public SkinnedMeshRenderer[] faceMeshes; // Added for public access in Inspector

        private readonly MeshType[] faceMeshTypes = { MeshType.HeadMesh, MeshType.BeardMesh, MeshType.TeethMesh };
        private bool CanGetAmplitude => AudioSource != null && AudioSource.clip != null && AudioSource.isPlaying;

        private CancellationTokenSource ctxSource;

        private void Start()
        {
            CreateBlendshapeMeshMap();
            if (!HasMouthOpenBlendshape())
            {
                Debug.LogWarning(MISSING_BLENDSHAPE_MESSAGE);
                enabled = false;
                return;
            }
            ctxSource = new CancellationTokenSource();
#if UNITY_IOS
            CheckIOSMicrophonePermission(ctxSource.Token);
#elif UNITY_ANDROID
            CheckAndroidMicrophonePermission(ctxSource.Token);
#elif UNITY_STANDALONE || UNITY_EDITOR
            InitializeAudio();
#endif
        }

        private bool HasMouthOpenBlendshape()
        {
            foreach (KeyValuePair<SkinnedMeshRenderer, int> blendshapeMeshIndex in blendshapeMeshIndexMap)
            {
                if (blendshapeMeshIndex.Value >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private void CreateBlendshapeMeshMap()
        {
            blendshapeMeshIndexMap = new Dictionary<SkinnedMeshRenderer, int>();
            foreach (MeshType faceMeshType in faceMeshTypes)
            {
                SkinnedMeshRenderer faceMesh = gameObject.GetMeshRenderer(faceMeshType);
                if (faceMesh)
                {
                    TryAddSkinMesh(faceMesh);
                }
            }
            // Add any manually assigned face meshes from Inspector
            if (faceMeshes != null)
            {
                foreach (SkinnedMeshRenderer faceMesh in faceMeshes)
                {
                    if (faceMesh != null && !blendshapeMeshIndexMap.ContainsKey(faceMesh))
                    {
                        TryAddSkinMesh(faceMesh);
                    }
                }
            }
        }

        private void TryAddSkinMesh(SkinnedMeshRenderer skinMesh)
        {
            if (skinMesh != null)
            {
                var index = skinMesh.sharedMesh.GetBlendShapeIndex(MOUTH_OPEN_BLEND_SHAPE_NAME);
                blendshapeMeshIndexMap.Add(skinMesh, index);
            }
        }

        private void Update()
        {
            var value = GetAmplitude();
            SetBlendShapeWeights(value);
        }

        public void InitializeAudio()
        {
            try
            {
                if (AudioSource == null)
                {
                    AudioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
                }

                switch (AudioProvider)
                {
                    case AudioProviderType.Microphone:
                        SetMicrophoneSource();
                        break;
                    case AudioProviderType.AudioClip:
                        SetAudioClipSource();
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("LipSync.Initialize:/n" + e);
            }
        }

        private void SetMicrophoneSource()
        {
#if UNITY_WEBGL
            Debug.LogWarning(MICROPHONE_IS_NOT_SUPPORTED_IN_WEBGL);
#else
            AudioSource.clip = Microphone.Start(null, true, 1, MICROPHONE_FREQUENCY);
            AudioSource.loop = true;
            AudioSource.mute = true;
            AudioSource.Play();
#endif
        }

        private void SetAudioClipSource()
        {
            AudioSource.clip = AudioClip;
            AudioSource.loop = false;
            AudioSource.mute = false;
            AudioSource.Stop();
        }

        public void PlayCurrentAudioClip()
        {
            AudioSource.Play();
        }

        public void PlayAudioClip(AudioClip audioClip)
        {
            AudioClip = AudioSource.clip = audioClip;
            PlayCurrentAudioClip();
        }

        private float GetAmplitude()
        {
            if (CanGetAmplitude && AudioSource.clip.loadState == AudioDataLoadState.Loaded)
            {
                var currentPosition = AudioSource.timeSamples;
                var remaining = AudioSource.clip.samples - currentPosition;
                if (remaining >= 0 && remaining < AUDIO_SAMPLE_LENGTH)
                {
                    return 0f;
                }

                AudioSource.clip.GetData(audioSample, AudioSource.timeSamples);
                var amplitude = 0f;

                foreach (var sample in audioSample)
                {
                    amplitude += Mathf.Abs(sample);
                }

                return Mathf.Clamp01(amplitude / audioSample.Length * AMPLITUDE_MULTIPLIER);
            }

            return 0f;
        }

        #region Blend Shape Movement

    void SetBlendShapeWeights(float weight)
        {
            foreach (KeyValuePair<SkinnedMeshRenderer, int> blendshapeMeshIndex in blendshapeMeshIndexMap)
            {
                if (blendshapeMeshIndex.Value >= 0)
                {
                    blendshapeMeshIndex.Key.SetBlendShapeWeight(blendshapeMeshIndex.Value, weight);
                }
            }
        }

        #endregion

        #region Permissions

#if UNITY_IOS
        private async void CheckIOSMicrophonePermission(CancellationToken ctx)
        {
            var asyncOperation = Application.RequestUserAuthorization(UserAuthorization.Microphone);
            while (!asyncOperation.isDone && !ctx.IsCancellationRequested)
            {
                await Task.Yield();
            }

            if(ctx.IsCancellationRequested) return;
            
            if (Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                InitializeAudio();
            }
            else
            {
                CheckIOSMicrophonePermission(ctx);
            }
        }
#endif

#if UNITY_ANDROID
        private async void CheckAndroidMicrophonePermission(CancellationToken ctx)
        {
            Permission.RequestUserPermission(Permission.Microphone);

            while (!Permission.HasUserAuthorizedPermission(Permission.Microphone) && !ctx.IsCancellationRequested)
            {
                await Task.Yield();
            }

            if (ctx.IsCancellationRequested) return;

            InitializeAudio();
        }
#endif

        #endregion

        private void OnDestroy()
        {
            audioSample = null;
            if (ctxSource != null)
            {
                ctxSource.Cancel();
                ctxSource.Dispose();
                ctxSource = null;
            }
#if !UNITY_WEBGL
            if (AudioSource != null && AudioProvider == AudioProviderType.Microphone)
            {
                Microphone.End(null);
            }
#endif
        }
    }
}