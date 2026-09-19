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

        // Camera.main is cached internally by Unity, so this is cheap per frame.
        // Resolving every frame (rather than caching once) means we cannot latch
        // onto a remote player's camera during the window before PlayerNetworkSetup
        // disables it.
        Camera main = Camera.main;
        return main != null ? main.transform : null;
    }
}
