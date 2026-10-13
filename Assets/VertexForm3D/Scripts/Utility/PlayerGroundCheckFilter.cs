using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

namespace VertexFormCore
{
    /// <summary>
    /// Keeps the gravity ground check from mistaking the player's own body for the floor.
    ///
    /// XR Interaction Toolkit's GravityProvider decides "grounded" by sweeping a small sphere from
    /// the player's HEAD down to their feet. Anything solid it meets on the way, on a layer in its
    /// mask, counts as ground. The player's own hand colliders, watch boxes and avatar body sit in
    /// exactly that column. With the mask at XRI's default (51, which includes Default) the sweep
    /// hits them, reports grounded, and gravity never runs: turn flying off in mid-air and you
    /// simply stay there.
    ///
    /// (Before 2026-10-01 the prefabs had the mask at 0, which hid this — but a mask of 0 never
    /// finds the real floor either, which is what let players fall through it. The mask has to stay
    /// at 51; it is the player's own colliders that have to get out of the way.)
    ///
    /// So the local player's own solid colliders are moved to the built-in "Ignore Raycast" layer,
    /// which is outside the ground-check mask. They still collide and push things exactly as before
    /// and still render (cameras draw that layer); they just stop being "ground". Triggers and the
    /// CharacterController itself are left alone. Re-checked twice a second, so an avatar model or
    /// hand added after spawning is covered as well.
    ///
    /// Self-installing; nothing needs adding to a scene or prefab.
    /// </summary>
    public class PlayerGroundCheckFilter : MonoBehaviour
    {
        const int IgnoreRaycastLayer = 2;

        float _timer;
        GameObject _player;
        bool _reported;
        readonly List<Collider> _colliders = new List<Collider>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var host = new GameObject("[Player Ground Check Filter]");
            DontDestroyOnLoad(host);
            host.AddComponent<PlayerGroundCheckFilter>();
        }

        void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f)
                return;
            _timer = 0.5f;

            GameObject player = RoomManager.Instance != null ? RoomManager.Instance.localVRPlayer : null;
            if (player == null)
                return;

            if (player != _player)
            {
                _player = player;
                _reported = false;
            }

            Apply(player);
        }

        void Apply(GameObject player)
        {
            int groundMask = 0;
            foreach (GravityProvider gravity in player.GetComponentsInChildren<GravityProvider>(true))
                if (gravity != null)
                    groundMask |= gravity.sphereCastLayerMask.value;

            if (groundMask == 0)
                return;   // no gravity provider (desktop rigs may not have one), or it checks nothing

            // Never move a collider onto a layer the check still sees.
            if ((groundMask & (1 << IgnoreRaycastLayer)) != 0)
            {
                if (!_reported)
                {
                    _reported = true;
                    Debug.LogWarning("[PlayerGroundCheckFilter] The gravity ground check includes the Ignore Raycast " +
                                     "layer, so the player's own colliders cannot be hidden from it.");
                }
                return;
            }

            int moved = 0;
            _colliders.Clear();
            player.GetComponentsInChildren(true, _colliders);

            foreach (Collider c in _colliders)
            {
                if (c == null || c.isTrigger || c is CharacterController)
                    continue;

                int layer = c.gameObject.layer;
                if ((groundMask & (1 << layer)) == 0)
                    continue;

                c.gameObject.layer = IgnoreRaycastLayer;
                moved++;
            }

            if (moved > 0)
                Debug.Log($"[PlayerGroundCheckFilter] Moved {moved} of the local player's own collider(s) out of the " +
                          $"gravity ground check (mask {groundMask}), so the player's body is no longer taken for the floor.");
        }
    }
}
