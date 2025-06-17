using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityEngine.Video;

public enum ObjectType
{
    GameObject,
    Video,
    Audio,
    SpriteRenderer,
    Image,
    Texture,
    skybox
}
public class AddressableObjectLoader : MonoBehaviour
{
    public ObjectType objectType;
    public string addressableKey;
    public bool LoadOnStart = true;
    public AudioSource audioSource;
    public VideoPlayer videoPlayer;
    public Image image;
    public RawImage rawImage;
    public SpriteRenderer spriteRenderer;
    public GameObject m_gameObject;
    public Transform spawnParent;
    public Vector3 spawnPosition;
    public Vector3 spawnRotation;
    void Start()
    {
        if (LoadOnStart)
        {
            LoadAddressable();
        }
    }

    public void LoadAddressable()
    {
        if (!AddressablesDownloader.Instance.isCatelogUpdated)
        {
            Invoke(nameof(LoadAddressable), 1f);
            return;
        }
        switch (objectType)
        {
            case ObjectType.GameObject:
                LoadGameObjectFromAddressables(addressableKey, (o) =>
                {
                    if (m_gameObject != null)
                    {
                        Destroy(m_gameObject);
                    }
                    m_gameObject = Instantiate(o, spawnPosition, Quaternion.Euler(spawnRotation));
                    if (spawnParent != null)
                    {
                        m_gameObject.transform.SetParent(spawnParent);
                    }
                    m_gameObject.name = o.name;
                });
                break;
            case ObjectType.Video:
                LoadVideoFromAddressables(addressableKey, (v) =>
                {
                    videoPlayer.clip = v; videoPlayer.Play();
                });
                break;
            case ObjectType.Audio:
                LoadAudioFromAddressables(addressableKey, (a) =>
                {
                    audioSource.clip = a; audioSource.Play();
                });
                break;
            case ObjectType.SpriteRenderer:
                LoadSpriteInSpriteRenderFromAddressables(addressableKey, spriteRenderer);
                break;
            case ObjectType.Image:
                LoadSpriteInImageFromAddressables(addressableKey, image);
                break;
            case ObjectType.Texture:
                LoadTextureFromAddressables(addressableKey, (t) =>
                {
                    if (rawImage != null)
                    {
                        rawImage.texture = t;
                    }
                    else
                    {
                        Debug.LogWarning("RawImage is not assigned for Texture loading.");
                    }
                });
                break;
            case ObjectType.skybox:

                LoadMaterialFromAddressables(addressableKey, (m) =>
                {
                    RenderSettings.skybox = m;
                    DynamicGI.UpdateEnvironment();
                });

                break;
            default:
                break;
        }
    }

    #region TEXTURE
    public void LoadTextureFromAddressables(string materialKey, Action<Texture> onMaterialLoaded)
    {
        AsyncOperationHandle<Texture> handle = Addressables.LoadAssetAsync<Texture>(materialKey);

        handle.Completed += (operation) => OnTextureLoaded(operation, onMaterialLoaded);
    }

    public void OnTextureLoaded(AsyncOperationHandle<Texture> handle, Action<Texture> onTextureLoaded)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Texture tex = handle.Result;

            onTextureLoaded?.Invoke(tex);
        }
        else
        {
            Debug.LogError($"Failed to load Texture: {handle.DebugName}, Error: {handle.OperationException}");
        }
    }


    #endregion
    #region MATERIAL
    public void LoadMaterialFromAddressables(string materialKey, Action<Material> onMaterialLoaded)
    {
        AsyncOperationHandle<Material> handle = Addressables.LoadAssetAsync<Material>(materialKey);

        handle.Completed += (operation) => OnMaterialLoaded(operation, onMaterialLoaded);
    }


    public void OnMaterialLoaded(AsyncOperationHandle<Material> handle, Action<Material> onMaterialLoaded)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Material mat = handle.Result;

            onMaterialLoaded?.Invoke(mat);
        }
        else
        {
            Debug.LogError($"Failed to load Material: {handle.DebugName}, Error: {handle.OperationException}");
        }
    }
    #endregion

    #region SPRITE
    public void LoadSpriteInImageFromAddressables(string spriteKey, Image img)
    {
        Debug.LogError("LoadSprite: " + spriteKey);
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(spriteKey);

        handle.Completed += (operation) => OnAddressableSpriteLoaded(operation, (s) => { img.sprite = s; });
    }

    public void LoadSpriteInSpriteRenderFromAddressables(string spriteKey, SpriteRenderer spr)
    {
        Debug.LogError("LoadSprite: " + spriteKey);
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(spriteKey);

        handle.Completed += (operation) => OnAddressableSpriteLoaded(operation, (s) => { spr.sprite = s; });
    }

    public void OnAddressableSpriteLoaded(AsyncOperationHandle<Sprite> handle, Action<Sprite> spriteLoaded)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Sprite loadedSprite = handle.Result;
            spriteLoaded?.Invoke(loadedSprite);
        }
        else
        {
            Debug.LogError($"Failed to load sprite: {handle.DebugName}, Error: {handle.OperationException}");
        }
    }
    #endregion

    #region AUDIO

    public void LoadAudioFromAddressables(string audioKey, Action<AudioClip> onAudioclipLoaded)
    {
        AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(audioKey);

        handle.Completed += (operation) => OnAudioLoaded(operation, onAudioclipLoaded);
    }


    public void OnAudioLoaded(AsyncOperationHandle<AudioClip> handle, Action<AudioClip> onAudioclipLoaded)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            AudioClip audio = handle.Result;

            onAudioclipLoaded?.Invoke(audio);
        }
        else
        {
            Debug.LogError($"Failed to load Audio: {handle.DebugName}, Error: {handle.OperationException}");
        }
    }
    #endregion


    #region GAMEOBJECT

    public void LoadGameObjectFromAddressables(string gameObjKey, Action<GameObject> onGameObjectLoaded)
    {
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(gameObjKey);

        handle.Completed += (operation) => OnGameObjectLoaded(operation, onGameObjectLoaded);
    }


    public void OnGameObjectLoaded(AsyncOperationHandle<GameObject> handle, Action<GameObject> onGameObjectLoaded)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject gameObj = handle.Result;
            Debug.LogError("gameObj: " + gameObj.name);

            onGameObjectLoaded?.Invoke(gameObj);
        }
        else
        {
            Debug.LogError($"Failed to load GameObject: {handle.DebugName}, Error: {handle.OperationException}");
        }
    }
    #endregion

    #region VIDEO

    public void LoadVideoFromAddressables(string gameObjKey, Action<VideoClip> onVideoLoaded)
    {
        AsyncOperationHandle<VideoClip> handle = Addressables.LoadAssetAsync<VideoClip>(gameObjKey);

        handle.Completed += (operation) => OnVideoLoaded(operation, onVideoLoaded);
    }

    public void OnVideoLoaded(AsyncOperationHandle<VideoClip> handle, Action<VideoClip> onVideoLoaded)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            VideoClip video = handle.Result;

            onVideoLoaded?.Invoke(video);
        }
        else
        {
            Debug.LogError($"Failed to load Video: {handle.DebugName}, Error: {handle.OperationException}");
        }
    }
    #endregion

}

#if UNITY_EDITOR

[CustomEditor(typeof(AddressableObjectLoader))]
public class AddressableObjectLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Get the target script
        AddressableObjectLoader loader = (AddressableObjectLoader)target;

        // Always show these fields
        EditorGUILayout.PropertyField(serializedObject.FindProperty("objectType"), new GUIContent("Object Type"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("addressableKey"), new GUIContent("Addressable Key"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LoadOnStart"), new GUIContent("Load On Start"));

        // Show fields based on ObjectType
        switch (loader.objectType)
        {
            case ObjectType.GameObject:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_gameObject"), new GUIContent("Game Object"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnParent"), new GUIContent("Spawn Parent"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnPosition"), new GUIContent("Spawn Position"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnRotation"), new GUIContent("Spawn Rotation"));
                break;

            case ObjectType.Video:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("videoPlayer"), new GUIContent("Video Player"));
                break;

            case ObjectType.Audio:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("audioSource"), new GUIContent("Audio Source"));
                break;

            case ObjectType.SpriteRenderer:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spriteRenderer"), new GUIContent("Sprite Renderer"));
                break;

            case ObjectType.Image:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("image"), new GUIContent("Image"));
                break;

            case ObjectType.Texture:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rawImage"), new GUIContent("Texture"));
                // No specific fields for Texture in this case
                break;
        }

        // Apply changes to the serialized object
        serializedObject.ApplyModifiedProperties();
    }
}
#endif