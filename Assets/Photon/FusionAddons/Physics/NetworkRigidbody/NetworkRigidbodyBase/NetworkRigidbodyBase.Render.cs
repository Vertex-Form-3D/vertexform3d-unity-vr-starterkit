using UnityEngine;

namespace Fusion.Addons.Physics
{
  public abstract partial class NetworkRigidbody<RBType, PhysicsSimType> {

    // PhysX/Box2D abstractions

    /// <summary>
    /// Returns true if the passed Rigidbody/Rigidbody2D velocity energies are below the sleep threshold.
    /// </summary>
    protected abstract bool IsRigidbodyBelowSleepingThresholds(RBType rb);
    /// <summary>
    /// Returns true if the passed NetworkRBData velocity energies are below the sleep threshold.
    /// </summary>
    protected abstract bool IsStateBelowSleepingThresholds(NetworkRBData data);

    // NRB Render Logic

    /// <inheritdoc/>
    public override void Render() {
      // Specifically flagged to not interpolate for cached reasons (ie for Server (non-Host))
      if (_doNotInterpolate) {
        return;
      }

      var  isInSimulation         = Object.IsInSimulation;
      bool physicsSimulatorExists = (object)_physicsSimulator != null;

      // If Unity is auto-simulating, only non-simulated proxies should be interpolated
      if (isInSimulation && (physicsSimulatorExists == false || _physicsSimulator.PhysicsAuthority == PhysicsAuthorities.Unity)) {
        return;
      }

      var it = _interpolationTarget;
      var hasInterpolationTarget = (object)it != null;

      // Correct Interpolation is currently only valid for simulation in the FixedUpdateNetwork() timing,
      // as correct Interpolation for the FixedUpdate() timing would require a before FixedUpdate() reset similar to IBeforeAllTicks.
      // The primary use case for the FixedUpdate() timing is for Shared Mode in Single Peer mode, in which case
      // Unity should be the RunnerSimulatePhysics Physics Authority - and the Rigidbody Interpolation can just be enabled.
      // We are supporting interpolation here however so that Multi-Peer interpolation is still possible with the FixedUpdate timing.
      if (isInSimulation) {
        var physicsTiming =  _physicsSimulator.PhysicsTiming;
        switch (physicsTiming) {
          // FixedUpdate() timing can only interpolate if there is an interpolation target.
          // Interpolating by moving the root would require a proper state reset before any FixedUpdate() callbacks used by controller code.
          // Note: Interpolation will be imperfect for objects simulated in FixedUpdate() as aliasing effects may occur
          // due to FixedUpdate() sim results and FixedUpdateNetwork() based state capture not perfectly aligning.
          case PhysicsTimings.FixedUpdate when hasInterpolationTarget == false:
            return;

          // Update-based movement should NEVER interpolate, it is always inherently in the Render timeframe.
          case PhysicsTimings.Update when Object.IsProxy == false:
            return;
        }
      }

      // Do not interpolate if Object setting indicates not to.
      if (Object.RenderSource == RenderSource.Latest) {
        return;
      }

      var tr                      = _transform;
      var useTarget               = isInSimulation && hasInterpolationTarget;

      if (TryGetSnapshotsBuffers(out var fr, out var to, out var alpha)) {

        var frData = fr.ReinterpretState<NetworkRBData>();
        var toData = to.ReinterpretState<NetworkRBData>();

        var frKey     = frData.TRSPData.TeleportKey;
        var toKey     = toData.TRSPData.TeleportKey;
        var syncScale = SyncScale;

        // cache the from values for position and rotation as these will almost certainly be needed below.
        var frPosition = frData.TRSPData.Position;
        var frRotation = UsePreciseRotation ? frData.FullPrecisionRotation : frData.TRSPData.Rotation;

        var syncParent    = SyncParent;
        var useWorldSpace = !syncParent;
        var teleport      = frKey != toKey;

        // Teleport Handling - Don't interpolate through non-moving teleports (indicated by positive key values).
        if (teleport && toKey >= 0) {
          toData = frData;
        }

        // Parenting specific handling

        if (syncParent) {
          var currentParent = tr.parent;

          // If the parent is a non-null... (either valid or Non-Networked)
          if (frData.TRSPData.Parent != default) {

            bool frHasNonNetworkedParent = frData.TRSPData.Parent == NetworkTRSPData.NonNetworkedParent;

            if (frHasNonNetworkedParent) {
              useWorldSpace = true;
              // Do nothing. We can't look up the old non-networked parent here.
            } else  if (Runner.TryFindBehaviour(frData.TRSPData.Parent, out var found)) {
              var foundParent = found.transform;
              // Set the parent if it currently is not correct, before moving
              if (currentParent != foundParent) {
                tr.SetParent(foundParent);
                _rootIsDirtyFromInterpolation = true;
              }

            } else {
              UnityEngine.Debug.LogError($"Parent of this object is not present {frData.TRSPData.Parent} {frData.TRSPData.Parent.Behaviour}.");
              return;
            }

            // switching to moving by root while parented (and kinematic), set the interpolation target to origin
            // We most move by the root because all TRSP is in Local Space, and the interpolation target needs to be positioned
            // in World Space.
            if (it) {
              it.localPosition = default;
              it.localRotation = Quaternion.identity;
              if (SyncScale) {
                _interpolationTarget.localScale = new Vector3(1f, 1f, 1f);
              }
              _targIsDirtyFromInterpolation = false;
            }

            // If the parent changes between From and To ... do no try to interpolate (different spaces)
            // We also are skipping sleep detection and teleport testing.
            if (frData.TRSPData.Parent != toData.TRSPData.Parent) {
              // For Non-Networked parents we use world space
              // When parented, ignore any specified interpolation target and move the NO transform always
              // (devs may want to change this behaviour themselves for edge cases)
              if (useWorldSpace) {
                tr.SetPositionAndRotation(frPosition, frRotation);
              } else {
                tr.localPosition = frPosition;
                tr.localRotation = frRotation;
              }
              if (syncScale) {
                tr.localScale = frData.TRSPData.Scale;
              }
              _rootIsDirtyFromInterpolation = true;
              return;
            }

            // If there is a parent, ignore the interpolation target.
            useTarget = false;
          } else {
            // else the parent is null
            if (currentParent != null) {
              tr.SetParent(null);
              _rootIsDirtyFromInterpolation = true;
            }
            // If the parent changes between From and To ... do no try to interpolate (different spaces)
            if (frData.TRSPData.Parent != toData.TRSPData.Parent) {
              if (useTarget) {
                // There is no parent, so we can safely move the interp target in world space.
                // HOWEVER if developers move the object in LateUpdate this will break of course.
                it.SetPositionAndRotation(frPosition, frRotation);

#if UNITY_EDITOR
                if (syncScale) {
                  UnityEngine.Debug.LogWarning($"{GetType().Name} cannot sync scale when using an interpolation target.");
                }
#endif
                _targIsDirtyFromInterpolation = true;
              } else {
                tr.localPosition = frPosition;
                tr.localRotation = frRotation;
                if (syncScale) {
                  tr.localScale = frData.TRSPData.Scale;
                }
                _rootIsDirtyFromInterpolation = true;
              }
              return;
            }
          }
        }

        // General Positon/Rotation Rendering

        Vector3    pos;
        Quaternion rot;

        if (teleport && toKey < 0) {
          // for moving teleports, lerp toward the Teleport values.
          pos = Vector3.Lerp(    frData.TRSPData.Position, toData.TeleportPosition, alpha);
          rot = Quaternion.Slerp(frRotation, toData.TeleportRotation, alpha);
        } else {
          pos = Vector3.Lerp(    frPosition, toData.TRSPData.Position, alpha);
          rot = Quaternion.Slerp(frRotation, UsePreciseRotation ? toData.FullPrecisionRotation : toData.TRSPData.Rotation, alpha);
        }

        // If we are using the interpolation target, just move the root of the target in world space. No scaling (they are invalid).
        if (useTarget) {
          it.SetPositionAndRotation(pos, rot);
          // SyncScale when using interpolation targets is always suspect, but we are allowing it here in case the dev has done things correctly.
          if (syncScale) {
            var scl = Vector3.Lerp(frData.TRSPData.Scale, toData.TRSPData.Scale, alpha);
            it.localScale = scl;
          }
          _targIsDirtyFromInterpolation = true;
        }
        // else (no interpolation target set) we are moving the transform itself and not the interp target.
        else {

          var scl = syncScale ? Vector3.Lerp(frData.TRSPData.Scale, toData.TRSPData.Scale, alpha) :  default;

          // Check thresholds to see if this object is coming to a rest, and stop interpolation to allow for sleep to occur.
          // Don't apply Pos/Rot/Scl if all of the indicated tests test below thresholds.
          if (!hasInterpolationTarget && !_targIsDirtyFromInterpolation && UseRenderSleepThresholds) {
            var thresholds = RenderThresholds;
            if (
              (!thresholds.UseEnergy    || IsStateBelowSleepingThresholds(frData))                                                       &&
              (thresholds.Position == 0 || (pos - tr.position).sqrMagnitude                 < thresholds.Position * thresholds.Position) &&
              (thresholds.Rotation == 0 || Quaternion.Angle(rot, tr.rotation)               < thresholds.Rotation)                       &&
              (thresholds.Scale    == 0 || !syncScale || (scl - tr.localScale).sqrMagnitude < thresholds.Scale * thresholds.Scale)) {
              return;
            }
          }

          if (useWorldSpace) {
            tr.SetPositionAndRotation(pos, rot);
          } else {
            tr.localPosition = pos;
            tr.localRotation = rot;
          }
          if (syncScale) {
            tr.localScale = scl;
          }
          _rootIsDirtyFromInterpolation = true;
        }

      } else {
        Debug.LogWarning($"No interpolation data");
      }
    }

  }
}
