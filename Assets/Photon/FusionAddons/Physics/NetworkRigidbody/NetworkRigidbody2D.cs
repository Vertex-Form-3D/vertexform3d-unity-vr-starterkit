using UnityEngine;

namespace Fusion.Addons.Physics {

  /// <summary>
  /// Fusion synchronization component for Unity Rigidbody2D.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Rigidbody2D))]
  [NetworkBehaviourWeaved(NetworkRBData.WORDS)]
  public class NetworkRigidbody2D : NetworkRigidbody<Rigidbody2D, RunnerSimulatePhysics2D> {

    /// <inheritdoc/>
    public override Vector3 RBPosition {
      get => _rigidbody.position;
      set => _rigidbody.position = value;
    }
    /// <inheritdoc/>
    public override Quaternion RBRotation {
      get => Quaternion.Euler(0, 0, _rigidbody.rotation);
      set => _rigidbody.rotation = value.eulerAngles.z;
    }

    /// <inheritdoc/>
    public override bool RBIsKinematic {
#if UNITY_6000_0_OR_NEWER
      get => _rigidbody.bodyType == RigidbodyType2D.Kinematic;
      set => _rigidbody.bodyType = value ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
#else
      get => _rigidbody.isKinematic;
      set => _rigidbody.isKinematic = value;
#endif
    }

    /// <inheritdoc/>
    protected override void Awake() {
      base.Awake();
      AutoSimulateIsEnabled = Physics2D.simulationMode != SimulationMode2D.Script;
    }
    /// <inheritdoc/>
    protected override bool GetRBIsKinematic(Rigidbody2D rb) {
      return RBIsKinematic;
    }
    /// <inheritdoc/>
    protected override void SetRBIsKinematic(Rigidbody2D rb, bool kinematic) {
      if (RBIsKinematic != kinematic) {
        RBIsKinematic = kinematic;
      }
    }

    /// <inheritdoc/>
    protected override void CaptureRBPositionRotation(Rigidbody2D rb, ref NetworkRBData data, bool useWorldSpace) {
      
      var pos = useWorldSpace ? (Vector3)rb.position : rb.transform.localPosition;
      var rot = useWorldSpace ? rb.rotation : rb.transform.eulerAngles.z;

      data.TRSPData.Position = pos;
      if (UsePreciseRotation) {
        data.FullPrecisionRotation = Quaternion.Euler(0, 0, rot);
      } else {
        data.TRSPData.Rotation = Quaternion.Euler(0, 0, rot);
      }
    }
    /// <inheritdoc/>
    protected override void ApplyRBPositionRotation(Rigidbody2D rb, Vector3 pos, Quaternion rot) {
      rb.position = pos;
      rb.rotation = rot.eulerAngles.z;
    }

    /// <inheritdoc/>
    protected override NetworkRigidbodyFlags GetRBFlags(Rigidbody2D rb) {
      var flags = default(NetworkRigidbodyFlags);
      if (RBIsKinematic)  { flags |= NetworkRigidbodyFlags.IsKinematic; }
      if (rb.IsSleeping()) { flags |= NetworkRigidbodyFlags.IsSleeping; }
      return flags;
    }
    /// <inheritdoc/>
    protected override int GetRBConstraints(Rigidbody2D rb) {
      return (int)rb.constraints;
    }
    /// <inheritdoc/>
    protected override void SetRBConstraints(Rigidbody2D rb, int constraints) {
      rb.constraints = (RigidbodyConstraints2D)constraints;
    }

    /// <inheritdoc/>
    protected override void CaptureExtras(Rigidbody2D rb, ref NetworkRBData data) {
      data.Mass              = rb.mass;
#if UNITY_6000_0_OR_NEWER
      data.Drag              = rb.linearDamping;
      data.AngularDrag       = rb.angularDamping;
      data.LinearVelocity    = rb.linearVelocity;
#else
      data.Drag              = rb.drag;
      data.AngularDrag       = rb.angularDrag;
      data.LinearVelocity    = rb.velocity;
      data.AngularVelocity2D = rb.angularVelocity;
      data.GravityScale2D    = rb.gravityScale;
#endif
    }

    /// <inheritdoc/>
    protected override void ApplyExtras(Rigidbody2D rb, ref NetworkRBData data) {
      rb.mass            = data.Mass;
#if UNITY_6000_0_OR_NEWER
      rb.angularDamping = data.AngularDrag;
      rb.linearDamping            = data.Drag;
      rb.linearVelocity        = data.LinearVelocity;
#else
      rb.angularDrag     = data.AngularDrag;
      rb.drag            = data.Drag;
      rb.velocity        = data.LinearVelocity;
#endif
      rb.angularVelocity = data.AngularVelocity.Z;
      rb.gravityScale    = data.GravityScale2D;
    }

    /// <inheritdoc/>
    public override void ResetRigidbody() {
      base.ResetRigidbody();
      var rb = _rigidbody;
#if UNITY_6000_0_OR_NEWER
      rb.linearVelocity        = default;
#else
      rb.velocity        = default;
#endif
      rb.angularVelocity = default;
    }

    /// <inheritdoc/>
    protected override bool IsRBSleeping(Rigidbody2D rb) => rb.IsSleeping();
    /// <inheritdoc/>
    protected override void ForceRBSleep(Rigidbody2D rb) => rb.Sleep();
    /// <inheritdoc/>
    protected override void ForceRBWake( Rigidbody2D rb) => rb.WakeUp();

    /// <inheritdoc/>
    protected override bool IsRigidbodyBelowSleepingThresholds(Rigidbody2D rb) {
      // Linear threshold

      float sqrMag;
#if UNITY_6000_0_OR_NEWER
      sqrMag = rb.linearVelocity.sqrMagnitude;
#else
      sqrMag = rb.velocity.sqrMagnitude;
#endif
      
      if (sqrMag > Physics2D.linearSleepTolerance * Physics2D.linearSleepTolerance) {
        return false;
      }

      // Angular threshold
      var angularVel = rb.angularVelocity;
      return angularVel * angularVel <= Physics2D.angularSleepTolerance * Physics2D.angularSleepTolerance;
    }

    /// <inheritdoc/>
    protected override bool IsStateBelowSleepingThresholds(NetworkRBData data) {
      // Linear threshold
      if (((Vector2)data.LinearVelocity).sqrMagnitude > Physics2D.linearSleepTolerance * Physics2D.linearSleepTolerance) {
        return false;
      }

      // Angular threshold
      var angularVel = data.AngularVelocity2D;
      return angularVel * angularVel <= Physics2D.angularSleepTolerance * Physics2D.angularSleepTolerance;
    }

  }
}
