using UnityEngine;

namespace Fusion.Addons.Physics {

  /// <summary>
  /// Base class for NRB which contains no physics references.
  /// </summary>
  public abstract partial class NetworkRigidbodyBase : NetworkTRSP, INetworkTRSPTeleport {
    protected new ref NetworkRBData Data => ref ReinterpretState<NetworkRBData>();

    /// <summary>
    /// Enables synchronization of Scale. Keep this disabled if you are not altering the scale of this transform, to reduce CPU utilization.
    /// </summary>
    [InlineHelp]
    [SerializeField]
    public bool SyncScale;

    /// <summary>
    /// Enables synchronization of Parent. Keep this disabled if you are not altering the parent of this transform, to reduce CPU utilization.
    /// </summary>
    [InlineHelp]
    [SerializeField]
    public bool SyncParent = true;

    /// <summary>
    /// Designate a render-only (non-physics) target Transform for all interpolation.
    /// </summary>
    [InlineHelp]
    [SerializeField]
    protected Transform _interpolationTarget;

    /// <summary>
    /// When disabled, rotation is stored in the <see cref="NetworkTRSPData"/> rotation field, which compresses rotation to 32 bits using 'Smallest Three'.
    /// When enabled, this <see cref="NetworkTRSPData"/> rotation field is not used.
    /// Instead, rotation only uses a separate uncompressed Quaternion field which otherwise is only used to
    /// store resting values when the RB goes to sleep.
    /// </summary>
    [InlineHelp]
    [SerializeField]
    public bool UsePreciseRotation;

    /// <summary>
    /// Enable checks which prevent interpolation from moving the root transform during interpolation unless needed.
    /// This mitigates the issue of Physics being broken by interpolating RBs by moving the Rigidbody's transform.
    /// Only applicable if no <see cref="_interpolationTarget"/> is designated.
    /// </summary>
    [InlineHelp]
    [Space]
    [SerializeField]
    [DrawIf(nameof(_interpolationTarget), false, CompareOperator.IsZero, DrawIfMode.Hide)]
    public bool UseRenderSleepThresholds = true;

    /// <summary>
    /// Render Sleep Threshold settings.
    /// </summary>
    [InlineHelp] [SerializeField] [DrawIf(nameof(_showSleepOptions), true, mode: DrawIfMode.Hide)]
    public TRSThresholds RenderThresholds = TRSThresholds.Default;

    // used by DrawIf attribute for inspector
    protected bool _showSleepOptions => !_interpolationTarget && UseRenderSleepThresholds;

    // Cached

    /// <summary>
    /// Cached transform reference.
    /// </summary>
    protected Transform _transform;

    /// <summary>
    /// Cached value indicating whether interpolation should occur.
    /// </summary>
    protected bool _doNotInterpolate;
    /// <summary>
    /// Cached value indicating if client prediction applies to this NetworkObject on this client.
    /// </summary>
    protected bool _clientPrediction;
    /// <summary>
    /// Dirty flag for the root Transform.
    /// True when interpolation has altered the root transform's position, rotation, or scale in Render().
    /// Is reset to false when the transform is restored to its networked state during the simulation loop.
    /// </summary>
    protected bool _rootIsDirtyFromInterpolation;
    /// <summary>
    /// Dirty flag for the Interpolation Target.
    /// True when interpolation has altered the position, rotation, or scale in Render().
    /// Is reset to false when the transform is restored to defaults during the simulation loop.
    /// </summary>
    protected bool _targIsDirtyFromInterpolation;
    /// <summary>
    /// Cached Runner.Config.Simulation.AreaOfInterestEnabled value.
    /// </summary>
    protected bool _aoiEnabled;
    /// <summary>
    /// Cached Physics.autoSimulation (or Physics.simulationMode != SimulationMode.Script in 2022.3 or higher) value.
    /// </summary>
    protected bool AutoSimulateIsEnabled { get; set; }

    /// <summary>
    /// Get/Set the Transform (typically a child of the Rigidbody root transform) which will be moved in interpolation.
    /// When set to null, the Rigidbody Transform will be used.
    /// </summary>
    public Transform InterpolationTarget {
      get => _interpolationTarget;
      set => SetInterpolationTarget(value);
    }

    /// <summary>
    /// Change the Transform (typically a child of the Rigidbody root transform) which will be moved in interpolation.
    /// When set to null, the Rigidbody Transform will be used.
    /// </summary>
    public void SetInterpolationTarget(Transform target) {
      if (target == null || target == transform) {
        _interpolationTarget          = null;
        _targIsDirtyFromInterpolation = false;
      } else {
#if UNITY_EDITOR
        var c = target.GetComponentInChildren<Collider>();
        if (c && c.enabled) {
          Debug.LogWarning($"Assigned Interpolation Target '{target.name}' on GameObject '{name}' contains a non-trigger collider, this may not be intended as interpolation may break physics caching, and prevent the Rigidbody from sleeping");
        }
#endif
        _interpolationTarget    = target;
      }
    }

    /// <summary>
    /// Unity's OnValidate call.
    /// </summary>
    protected virtual void OnValidate() {
      SetInterpolationTarget(_interpolationTarget);
    }
    /// <inheritdoc/>
    public override void Spawned() {
      _aoiEnabled = Runner.Config.Simulation.AreaOfInterestEnabled;

      // Don't interpolate on Dedicated Server
      _doNotInterpolate = Runner.Mode == SimulationModes.Server;

      // Validate the serialized target.
      SetInterpolationTarget(_interpolationTarget);

      RBPosition = transform.position;
      RBRotation = transform.rotation;
    }

    /// <summary>
    /// Initiate a moving teleport. This method must be in FixedUpdateNetwork() called before
    /// <see cref="RunnerSimulatePhysics3D"/> and <see cref="RunnerSimulatePhysics2D"/> have simulated physics.
    /// This teleport is deferred until after physics has simulated, and captures position and rotation values both before and after simulation.
    /// This allows interpolation leading up to the teleport to have a valid pre-teleport TO target.
    /// </summary>
    public abstract void Teleport(Vector3? position = null, Quaternion? rotation = null);
  }

}
