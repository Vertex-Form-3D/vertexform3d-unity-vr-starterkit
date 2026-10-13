using UnityEngine;

public class PlayerTag : MonoBehaviour
{
    [Tooltip("Optional. Leave empty to use the local camera automatically. " +
             "A camera that lives inside this player's own prefab is ignored, " +
             "because on a remote avatar that is THAT player's camera, not ours.")]
    [SerializeField] Transform _cameraTransform;

    [Tooltip("Keep the tag perfectly upright instead of tilting toward the viewer. " +
             "Recommended in VR, where you often look down at other players.")]
    [SerializeField] bool _keepUpright = true;

    bool _overrideChecked;
    bool _overrideUsable;

    void LateUpdate()
    {
        Transform viewer = ResolveViewer();
        if (viewer == null)
            return;

        // Point the tag's +Z AWAY from the viewer. Unity renders text readable
        // when its forward axis points away from the eye; LookAt(camera) would
        // point +Z at the viewer and render the text mirrored.
        Vector3 away = transform.position - viewer.position;

        if (_keepUpright)
            away.y = 0f;

        if (away.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(away, Vector3.up);
    }

    Transform ResolveViewer()
    {
        // A serialized override is only meaningful if it points at something
        // OUTSIDE this player's hierarchy. Anything inside is this avatar's own
        // camera, which is correct for the local player purely by coincidence
        // and wrong for every remote player.
        if (!_overrideChecked)
        {
            _overrideChecked = true;
            _overrideUsable = _cameraTransform != null
                              && !_cameraTransform.IsChildOf(transform.root);

            if (_cameraTransform != null && !_overrideUsable)
            {
                _cameraTransform = null;
                Debug.LogWarning(
                    $"[PlayerTag] '{transform.root.name}' had _cameraTransform wired to a " +
                    "transform inside its own prefab. Ignoring it and using the local camera. " +
                    "Clear that field in the prefab to silence this warning.", this);
            }
        }

        if (_overrideUsable)
            return _cameraTransform;

        // Resolved every frame rather than cached once, so we cannot latch onto a remote
        // player's camera during the window before PlayerNetworkSetup disables it.
        Camera viewer = ResolveViewerCamera();
        return viewer != null ? viewer.transform : null;
    }

    /// <summary>
    /// The camera the local person is actually looking through.
    ///
    /// <see cref="Camera.main"/> is tried first and is correct in VR, where the headset camera
    /// carries the MainCamera tag. It is NOT enough on its own: the desktop third-person view
    /// renders through OrbitCamera, which is deliberately Untagged, so Camera.main there is
    /// either null or some other rig's camera. A tag aimed at a camera nobody is looking
    /// through renders back-to-front, which is why desktop names came out mirrored.
    ///
    /// The fallback is simply whatever is drawing the screen: the enabled camera with the
    /// highest depth that is not rendering into a texture. Cameras that DO render into a
    /// texture — the mirror, the selfie stick — are excluded, since facing those would be just
    /// as wrong as facing nothing.
    /// </summary>
    static Camera ResolveViewerCamera()
    {
        Camera main = Camera.main;
        if (IsViewerCamera(main))
            return main;

        Camera best = null;
        Camera[] all = Camera.allCameras;
        for (int i = 0; i < all.Length; i++)
        {
            if (!IsViewerCamera(all[i]))
                continue;

            if (best == null || all[i].depth > best.depth)
                best = all[i];
        }

        return best;
    }

    static bool IsViewerCamera(Camera cam)
    {
        return cam != null
               && cam.isActiveAndEnabled
               && cam.targetTexture == null;
    }
}
