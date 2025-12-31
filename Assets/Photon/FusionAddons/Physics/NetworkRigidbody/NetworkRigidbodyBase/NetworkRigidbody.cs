using UnityEngine;

namespace Fusion.Addons.Physics {
  /// <summary>
  /// NetworkRigidbody base class with generic definition for the Unity Rigidbody type (3d or 2d) and
  /// <see cref="RunnerSimulatePhysicsBase{TPhysicsScene}"/> type.
  /// </summary>
  public abstract partial class NetworkRigidbody<RBType, PhysicsSimType> : NetworkRigidbodyBase, IStateAuthorityChanged, ISimulationExit, IAfterSpawned
    where RBType          : Component
    where PhysicsSimType  : RunnerSimulatePhysicsBase {

    /// <summary>
    /// Abstracted getter for cached Rigidbody component reference.
    /// </summary>
    public RBType Rigidbody => _rigidbody;

    // Cached

    /// <summary>
    /// Cached Rigidbody reference.
    /// </summary>
    protected RBType         _rigidbody;
    /// <summary>
    /// Cached reference of associated <see cref="RunnerSimulatePhysics3D"/> or <see cref="RunnerSimulatePhysics2D"/>.
    /// </summary>
    protected PhysicsSimType _physicsSimulator;
    /// <summary>
    /// Stored original kinematic setting of the Rigidbody.
    /// </summary>
    protected bool           _originalIsKinematic;

    /// <summary>
    /// Implementation of Unity Awake() method.
    /// </summary>
    protected virtual void Awake() {
      TryGetComponent(out _transform);
      TryGetComponent(out _rigidbody);

      // Store the original state. Used in Despawn to reset for pooling.
      _originalIsKinematic = RBIsKinematic;
    }

    void ISimulationExit.SimulationExit() {
      // RBs removed from simulation will stop getting Copy calls, and will only be running Render
      // So we need to set them as kinematic here (to avoid relentless checks in Render)
      SetRBIsKinematic(_rigidbody, true);
    }

    /// <inheritdoc/>
    public override void Spawned() {
      base.Spawned();

      // Force Proxies to be kinematic.
      // This can be removed if you specifically instruct proxies to simulate with Runner.SetIsSimulated()
      // and want proxies to predict (not applicable to Shared Mode).
      if (IsProxy) {
        SetRBIsKinematic(_rigidbody, true);
      }

      EnsureHasRunnerSimulatePhysics();
      _clientPrediction = Runner.Topology != Topologies.Shared && (Runner.IsServer || _physicsSimulator.ClientPhysicsSimulation == ClientPhysicsSimulation.SimulateAlways || _physicsSimulator.ClientPhysicsSimulation == ClientPhysicsSimulation.SimulateForward);

      if (HasStateAuthority) {
        CopyToBuffer(false);
      } else {
        // Mark the root as dirty to force CopyToEngine to update the transform.
        _rootIsDirtyFromInterpolation = true;
        CopyToEngine(true);
        // This has to be here after CopyToEngine, or it will set Kinematic right back.
        if (Object.IsInSimulation == false) {
          SetRBIsKinematic(_rigidbody, true);
        }
      }
    }
    
    public void AfterSpawned()
    {
      // Warn about incompatible configuration.
      if (Runner.IsClient)
      {
        if (Runner.Topology != Topologies.Shared)
        {
          if (Object.IsInSimulation && _clientPrediction == false)
          {
            Log.Warn($"The NetworkRigidbody [Id:{Object.Id}] is simulated on the local client fusion simulation. However, the client physics mode of RunnerSimulatePhysics is NOT set to predict local physics (ForwardOnly or Always simulate). Remove the NetworkObject from the simulation calling Runner.SetIsSimulated(Object, false); in Spawned()");
          }
          else if (_clientPrediction && Object.IsInSimulation == false)
          {
            Log.Warn($"The NetworkRigidbody [Id:{Object.Id}] is NOT simulated on the local client fusion simulation. However, the client physics mode of RunnerSimulatePhysics is set to predict local physics (ForwardOnly or Always simulate). Add the NetworkObject on the simulation calling Runner.SetIsSimulated(Object, true); in Spawned()");
          }
        }
      }
    }

    /// <inheritdoc/>
    public override void Despawned(NetworkRunner runner, bool hasState) {
      // Should not be possible but to avoid errors, check.
      if (_rigidbody)
      {
        ResetRigidbody();
      }
      
      base.Despawned(runner, hasState);
    }

    /// <summary>
    /// Reset velocity and other values to defaults, so that pooled objects do not Spawn()
    /// with previous velocities, etc.
    /// </summary>
    public virtual void ResetRigidbody() {
      SetRBIsKinematic(_rigidbody, _originalIsKinematic);
    }

    /// <summary>
    /// Implementation of <see cref="IStateAuthorityChanged"/> callback.
    /// </summary>
    public virtual void StateAuthorityChanged() {

      // This test exists because this callback currently fires on Scene Objects even if they are disabled.
      // May not be needed in the future if this behaviour in Fusion is changed.
      if (_rigidbody == false) {
        return;
      }

      // Debug.Log($"Auth Change {Runner.LocalPlayer} {name} {HasStateAuthority} {HasInputAuthority}");

      if (Object.IsProxy) {
        SetRBIsKinematic(_rigidbody, true);
      } else {
        // Apply complete state on the new Authority, to ensure velocities and extras apply to the now non-kinematic object.
        CopyToEngine(true);
      }
    }

    /// <summary>
    /// Tests if the NetworkRunner has the applicable
    /// <see cref="RunnerSimulatePhysics3D"/> or <see cref="RunnerSimulatePhysics2D"/> component.
    /// If not, adds one and applies a best guess for default settings.
    /// </summary>
    protected virtual void EnsureHasRunnerSimulatePhysics() {
      if (_physicsSimulator) {
        return;
      }

      if (Runner.TryGetComponent(out PhysicsSimType existing)) {
        _physicsSimulator = existing;
        return ;
      }

      // For Shared Mode in Single Peer mode, we by default will let Unity handle physics.
#if UNITY_2022_3_OR_NEWER
      var timing = (typeof(RBType) == typeof(Rigidbody) ? (PhysicsTimings)UnityEngine.Physics.simulationMode : (PhysicsTimings)Physics2D.simulationMode);
#else
      var timing = (typeof(RBType) == typeof(Rigidbody) ? (PhysicsTimings)(UnityEngine.Physics.autoSimulation ? PhysicsTimings.FixedUpdate : PhysicsTimings.Script) : (PhysicsTimings)Physics2D.simulationMode);
#endif

      // If all of the current mode settings allow for Unity to handled Physics(2D).Simulate() exit out
      if (Application.isPlaying                                           &&
          (bool)Runner                                                    &&
          Runner.IsRunning                                                &&
          Runner.Config.PeerMode == NetworkProjectConfig.PeerModes.Single &&
          (Runner.GameMode == GameMode.Shared)                            &&
          timing != PhysicsTimings.Script) {
        return;
      }

      Debug.LogWarning($"No {typeof(PhysicsSimType).Name} present on NetworkRunner, but is required by {GetType().Name} on gameObject '{name}'. Adding one using default settings.");
      _physicsSimulator = Runner.gameObject.AddComponent<PhysicsSimType>();
      Runner.AddGlobal(_physicsSimulator);
    }

    /// <summary>
    /// Developers can override this method to add handling for parent not existing locally.
    /// </summary>
    protected virtual void OnParentNotFound() {
      Debug.LogError($"Parent does not exist locally");
    }
  }
}
