using UnityEngine;
using UnityEngine.Rendering;
using ReadyPlayerMe.Core;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
public class LoadRPMAvatar : MonoBehaviour
{
    private const string AVATAR_ID_KEY = "RPM_AvatarID";
    private const string RPM_URL_PREFIX = "https://models.readyplayer.me/";
    private const string GLB_EXTENSION = ".glb";
    public string defaultAvatarUrl;
    [SerializeField] private AvatarConfig config;
    //[SerializeField] private SittingController sittingController;

    [Header("Shadow Proxy Settings")]
    [Tooltip("Enable to create a shadow-only duplicate that casts shadows while main avatar is on MirrorOnly layer")]
    [SerializeField] private bool useShadowProxy = true;

    [Tooltip("The shadow proxy GameObject (auto-created if null)")]
    [SerializeField] private GameObject shadowProxy;

    private Animator animator;
    private Transform leftEye;
    private Transform rightEye;
    private AvatarObjectLoader loader;
    private Dictionary<string, SkinnedMeshRenderer> shadowProxyRenderers = new Dictionary<string, SkinnedMeshRenderer>();

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component missing!", this);
            return;
        }

        CacheEyeBones();
        //LoadSavedAvatar();
    }

    private void OnDestroy()
    {
        if (loader != null)
        {
            loader.OnCompleted -= OnAvatarLoadCompleted;
            loader.Cancel();
        }
    }

    private void CacheEyeBones()
    {
        leftEye = AvatarBoneHelper.GetLeftEyeBone(transform, true);
        rightEye = AvatarBoneHelper.GetRightEyeBone(transform, true);

        if (leftEye == null || rightEye == null)
        {
            Debug.LogWarning("Eye bones not found - eye movement may not work properly", this);
        }
    }

    private void LoadSavedAvatar()
    {
        if (PlayerPrefs.HasKey(AVATAR_ID_KEY))
        {
            string savedId = PlayerPrefs.GetString(AVATAR_ID_KEY);
            if (!string.IsNullOrWhiteSpace(savedId))
            {
                LoadAvatar(savedId);
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(defaultAvatarUrl))
        {
            LoadAvatar(defaultAvatarUrl);
        }
        else
        {
            Debug.LogWarning("No saved avatar ID and no default URL provided", this);
        }
    }

    public void LoadAvatar(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogError("Attempted to load avatar with empty ID", this);
            return;
        }

        // Clean up any existing loader
        if (loader != null)
        {
            loader.OnCompleted -= OnAvatarLoadCompleted;
            loader.Cancel();
        }

        string url = id.StartsWith("http") ? id : $"{RPM_URL_PREFIX}{id}{GLB_EXTENSION}";
        SetPlayer(url);

        PlayerPrefs.SetString(AVATAR_ID_KEY, id);
        PlayerPrefs.Save();
    }

    private void SetPlayer(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogError("Attempted to load avatar with empty URL", this);
            return;
        }

        loader = new AvatarObjectLoader
        {
            AvatarConfig = config ?? new AvatarConfig() // Fallback to default config if null
        };

        loader.OnCompleted += OnAvatarLoadCompleted;
        loader.LoadAvatar(url);
        Debug.Log("Avatar URL:" + url);
    }

    private void OnAvatarLoadCompleted(object sender, CompletionEventArgs args)
    {
        if (args == null || args.Avatar == null)
        {
            Debug.LogError("Avatar load completed with null arguments", this);
            return;
        }
        try
        {
            // Update eye positions if bones exist
            if (leftEye != null && rightEye != null)
            {
                var newLeftEye = AvatarBoneHelper.GetLeftEyeBone(args.Avatar.transform, true);
                var newRightEye = AvatarBoneHelper.GetRightEyeBone(args.Avatar.transform, true);

                if (newLeftEye != null) leftEye.localPosition = newLeftEye.localPosition;
                if (newRightEye != null) rightEye.localPosition = newRightEye.localPosition;
            }

            AvatarMeshHelper.TransferMesh(args.Avatar, gameObject);

            // Transfer mesh to shadow proxy if enabled
            if (useShadowProxy)
            {
                TransferMeshToShadowProxy(args.Avatar);
            }

            Destroy(args.Avatar);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during avatar transfer: {e}", this);
        }

        /*if (args.Metadata.OutfitGender.Equals(OutfitGender.Feminine))
        {
            sittingController.SetAvatarGender(true);
            Debug.Log("Loaded Feminine Avatar");
        }
        else
        {
            sittingController.SetAvatarGender(false);
            Debug.Log("Loaded Masculine Avatar");
        }*/
    }

    #region Shadow Proxy Methods

    /// <summary>
    /// Transfers mesh data from source avatar to the shadow proxy.
    /// Shadow proxy casts shadows but is invisible to all cameras (ShadowsOnly mode).
    /// </summary>
    /// <param name="source">The source avatar with mesh data to transfer</param>
    private void TransferMeshToShadowProxy(GameObject source)
    {
        if (!useShadowProxy) return;

        // Create shadow proxy structure if it doesn't exist
        if (shadowProxy == null)
        {
            CreateShadowProxyStructure();
        }

        var sourceRenderers = source.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var sourceRenderer in sourceRenderers)
        {
            if (shadowProxyRenderers.TryGetValue(sourceRenderer.name, out var proxyRenderer))
            {
                // Transfer mesh and material
                proxyRenderer.sharedMesh = sourceRenderer.sharedMesh;
                proxyRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
            }
        }

        Debug.Log("[LoadRPMAvatar] Shadow proxy mesh transfer completed");
    }

    /// <summary>
    /// Creates the shadow proxy GameObject structure matching the main avatar's renderers.
    /// </summary>
    private void CreateShadowProxyStructure()
    {
        // Create shadow proxy root
        shadowProxy = new GameObject($"{gameObject.name}_ShadowProxy");
        shadowProxy.transform.SetParent(transform);
        shadowProxy.transform.localPosition = Vector3.zero;
        shadowProxy.transform.localRotation = Quaternion.identity;
        shadowProxy.transform.localScale = Vector3.one;

        // Keep on Default layer so main camera shadow pass includes it
        shadowProxy.layer = 0;

        // Get all skinned mesh renderers from main avatar
        var mainRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var mainRenderer in mainRenderers)
        {
            // Create a proxy renderer for each main renderer
            var proxyObj = new GameObject(mainRenderer.name);
            proxyObj.transform.SetParent(shadowProxy.transform);
            proxyObj.transform.localPosition = Vector3.zero;
            proxyObj.transform.localRotation = Quaternion.identity;
            proxyObj.transform.localScale = Vector3.one;
            proxyObj.layer = 0; // Default layer

            var proxyRenderer = proxyObj.AddComponent<SkinnedMeshRenderer>();

            // Share bones with the main avatar's renderer (they animate together)
            proxyRenderer.bones = mainRenderer.bones;
            proxyRenderer.rootBone = mainRenderer.rootBone;
            proxyRenderer.updateWhenOffscreen = mainRenderer.updateWhenOffscreen;

            // Configure for shadow-only rendering
            proxyRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            proxyRenderer.receiveShadows = false;
            proxyRenderer.lightProbeUsage = LightProbeUsage.Off;
            proxyRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            shadowProxyRenderers[mainRenderer.name] = proxyRenderer;
        }

        Debug.Log($"[LoadRPMAvatar] Created shadow proxy with {shadowProxyRenderers.Count} renderers");
    }

    /// <summary>
    /// Enables or disables the shadow proxy at runtime.
    /// </summary>
    public void SetShadowProxyEnabled(bool enabled)
    {
        useShadowProxy = enabled;

        if (shadowProxy != null)
        {
            shadowProxy.SetActive(enabled);
        }
    }

    /// <summary>
    /// Returns the shadow proxy GameObject.
    /// </summary>
    public GameObject GetShadowProxy()
    {
        return shadowProxy;
    }

    #endregion
}