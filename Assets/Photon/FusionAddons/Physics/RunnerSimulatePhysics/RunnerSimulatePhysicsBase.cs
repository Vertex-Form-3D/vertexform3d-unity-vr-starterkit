using System;
using System.Collections.Generic;
using Fusion.Analyzer;
using UnityEngine;

namespace Fusion.Addons.Physics {

  /// <summary>
  /// Base class for <see cref="RunnerSimulatePhysics3D"/> and <see cref="RunnerSimulatePhysics2D"/>;
  /// </summary>
  public abstract class RunnerSimulatePhysicsBase: SimulationBehaviour, IBeforeTick  {

    /// <summary>
    /// Stored original Physics auto-simulate setting, used to restore Unity settings when Fusion runners are shutdown.
    /// </summary>
    [StaticField(StaticFieldResetMode.None)]
    protected static PhysicsTimings _physicsAutoSimRestore;

    /// <summary>
    /// Tracked number of started NetworkRunners. Used to determine when last Runner has stopped,
    /// and original Unity physics settings should be restored.
    /// </summary>
    [StaticField(StaticFieldResetMode.None)]
    private static int _enabledRunnersCount;

    // Inspector logic (Used by our WarnIf and DrawIf attributes)

    /// <summary>
    /// Used by Fusion inspector UI.
    /// </summary>
    internal bool ShowClientPhysicsSimulation => _physicsTiming == PhysicsTimings.FixedUpdateNetwork;
    /// <summary>
    /// Used by Fusion inspector UI.
    /// </summary>
    internal bool ShowMultiplier => _physicsAuthority != PhysicsAuthorities.Unity;
    /// <summary>
    /// Used by Fusion inspector UI.
    /// </summary>
    internal bool WarnAutoSyncTransforms => AutoSyncTransforms && _physicsAuthority != PhysicsAuthorities.Unity && _physicsTiming != PhysicsTimings.Update;

    /// <summary>
    /// Indicates whether Unity or Fusion should handle Physics.Simulate() calls.
    /// When set to Auto (default), this will pick the most appropriate setting for the Game Mode and Peer Mode settings.
    /// </summary>
    [InlineHelp]
    [SerializeField]
    protected PhysicsAuthorities _physicsAuthority = PhysicsAuthorities.Fusion;
    /// <summary>
    /// Public getter of the <see cref="_physicsAuthority"/> value.
    /// Indicates whether Unity or Fusion should handle Physics.Simulate() calls.
    /// When set to Auto (default), this will pick the most appropriate setting for the Game Mode and Peer Mode settings.
    /// </summary>
    public    PhysicsAuthorities PhysicsAuthority => _physicsAuthority;

    /// <summary>
    /// Indicates which timing segment should be used for calling Physics.Simulate().
    /// </summary>
    [InlineHelp]
    [SerializeField]
    [DrawIf(nameof(_physicsAuthority), (long)PhysicsAuthorities.Unity, CompareOperator.NotEqual, Hide = true)]
    [WarnIf(nameof(WarnAutoSyncTransforms),
      "<b>AutoSyncTransforms</b> is enabled in Unity's Project Settings.\n\n"                                                                                               +
      "<i>This is potentially costly due to interpolation moving the Rigidbody transform every <b>Update()</b>. "                                                                    +
      "If you have <b>NetworkRigidbody</b> instances which do not have <b>InterpolationTarget</b> set, then it may be preferable to disable <b>AutoSyncTransforms</b> " +
      "and manually call <b>SyncTransforms()</b> before Raycast/Overlap queries.</i>",
      AsBox = true
    )]
    protected PhysicsTimings _physicsTiming = PhysicsTimings.FixedUpdateNetwork;
    /// <summary>
    /// Public getter of the <see cref="_physicsTiming"/> value.
    /// Indicates which timing segment should be used for calling Physics.Simulate().
    /// </summary>
    public    PhysicsTimings PhysicsTiming => _physicsTiming;

    /// <summary>
    /// Controls physics simulation on clients.<br/>
    /// Disabled - physics simulation doesn't run on clients. This value is default for performance reasons.<br/>
    /// SyncTransforms - clients call Physics.SyncTransforms() in all ticks.<br/>
    /// SimulateForward - clients call Physics.SyncTransforms() in resimulation ticks and Physics.Simulate() in forward ticks (a tick which is being simulated for the first time).<br/>
    /// SimulateAlways - clients call Physics.Simulate() in both resimulation and forward ticks. This option may introduce noticeable CPU overhead.
    /// </summary>
    [InlineHelp]
    [DrawIf(nameof(ShowClientPhysicsSimulation), Hide = true)]
    public ClientPhysicsSimulation ClientPhysicsSimulation = ClientPhysicsSimulation.Disabled;

    /// <summary>
    /// <para>This value is used to scale PhysicsSimulationDeltaTime, typically to speed up and slow down the passing of time.
    /// This doesn't change the Fusion TickRate (that value is fixed and cannot be changed once a game is started),
    /// and instead changes how much time is simulated each Physics tick. When changing this value, be sure to account for it
    /// in all code where you use <see cref="NetworkRunner.DeltaTime"/>, as you likely will want to apply the same modifier everywhere.</para>
    ///
    /// <para>For Physics.Simulate(deltaTime) - the deltaTime is calculated as PhysicsSimulationDeltaTime * DeltaTimeMultiplier.
    /// The resulting deltaTime must be a greater than zero value (You cannot simulate using zero or negative values).
    /// Values less than zero will be clamped to zero. Default is 1.
    /// A value of zero will result in Physics.Simulate not being called at all.</para>
    /// </summary>
    [InlineHelp]
    [DrawIf(nameof(ShowMultiplier), Hide = true)]
    [DisplayName("DeltaTime Multiplier")]
    [SerializeField]
    public float DeltaTimeMultiplier = 1;

    /// <summary>
    /// Sets Time.fixedDeltaTime to match Fusion.DeltaTime, ensuring that Unity is calling FixedUpdate
    /// at approx. the same interval that Fusion is calling FixedUpdateNetwork() forward Ticks
    /// </summary>
    [InlineHelp]
    [SerializeField]
    public bool SetUnityFixedTimestep = false;

    /// <summary>
    /// <para>DeltaTime used in FixedUpdateNetwork for Physics.Simulate(deltaTime).
    /// By default, returns <see cref="NetworkRunner.DeltaTime"/>.
    /// Override this if you want to control how much time passes in each tick (for bullet-time or time compression effects).
    /// You typically can just set the <see cref="DeltaTimeMultiplier"/> instead to speed up or slow down time.</para>
    ///
    /// <para>For Physics.Simulate(deltaTime) - the deltaTime is calculated as PhysicsSimulationDeltaTime * DeltaTimeMultiplier.
    /// The resulting deltaTime must be a greater than zero value (You cannot simulate using zero or negative values).
    /// Values less than zero will be clamped to zero. Default is 1.
    /// A value of zero will result in Physics.Simulate not being called at all. </para>
    /// </summary>
    public virtual float PhysicsSimulationDeltaTime {
      get => Runner.DeltaTime;
    }

    /// <summary>
    /// Abstracted get/set for Unity's Physics auto-sync transforms setting, for the applicable 3d/2d physics.
    /// </summary>
    protected abstract bool AutoSyncTransforms { get; set; }
    /// <summary>
    /// Abstracted getter for Unity's Physics physics mode setting, for the applicable 3d/2d physics.
    /// </summary>
    protected abstract PhysicsTimings UnityPhysicsMode { get; }
    /// <summary>
    /// Sets the auto-simulate setting for the associated Physics engine.
    /// If the setting is not currently overridden, the current value of the setting for the physics engine is recorded
    /// to allow for restoration later with the <see cref="RestoreAutoSimulate"/> method.
    /// </summary>
    protected abstract void OverrideAutoSimulate(bool enabled);
    /// <summary>
    /// Restores auto-simulate setting of the associated physics engine to its original value prior to any <see cref="OverrideAutoSimulate"/> method calls.
    /// </summary>
    protected abstract void RestoreAutoSimulate();

    #region Simulation Callbacks

    /// <summary>
    /// Callback invoked prior to Simulate() being called.
    /// </summary>
    public event Action OnBeforeSimulate;
    /// <summary>
    /// Callback invoked prior to Simulate() being called.
    /// </summary>
    public event Action OnAfterSimulate;

    // One-time callbacks
    private readonly Queue<Action> _onAfterSimulateCallbacks  = new Queue<Action>();
    private readonly Queue<Action> _onBeforeSimulateCallbacks = new Queue<Action>();

    /// <summary>
    /// Returns true if FixedUpdateNetwork has executed for the current tick, and physics has simulated.
    /// </summary>
    public bool HasSimulatedThisTick { get; private set; }

    /// <summary>
    /// Register a one time callback which will be called immediately before the next physics simulation occurs.
    /// Use <see cref="HasSimulatedThisTick"/> to determine if simulation has already happened.
    /// </summary>
    public void QueueBeforeSimulationCallback(Action callback) {
      _onBeforeSimulateCallbacks.Enqueue(callback);
    }
    /// <summary>
    /// Register a one time callback which will be called immediately after the next physics simulation occurs.
    /// Use <see cref="HasSimulatedThisTick"/> to determine if simulation has already happened.
    /// </summary>
    public void QueueAfterSimulationCallback(Action callback) {
      _onAfterSimulateCallbacks.Enqueue(callback);
    }

#endregion

    /// <summary>
    /// Method which calls Simulate() for the associated Unity physics engine,
    /// for the primary physics scene of the associated <see cref="NetworkRunner"/>.
    /// </summary>
    protected abstract void SimulatePrimaryScene(    float deltaTime);
    /// <summary>
    /// Method which calls Simulate() for the associated Unity physics engine,
    /// for any additional physics scenes of the associated <see cref="NetworkRunner"/>.
    /// </summary>
    protected abstract void SimulateAdditionalScenes(float deltaTime, bool checkPhysicsSimulation);

#if UNITY_EDITOR
    private void OnValidate() {
      if (_physicsTiming == PhysicsTimings.FixedUpdateNetwork && _physicsAuthority == PhysicsAuthorities.Unity) {
        Debug.LogWarning($"Unity cannot auto-simulate FixedUpdateNetwork(). Changing {nameof(_physicsAuthority)} to {PhysicsAuthorities.Auto}.");
        _physicsAuthority = PhysicsAuthorities.Auto;
      }
    }
#endif

    private bool _isInitialized;

    /// <summary>
    /// Initialization code that is run on the first execution of <see cref="FixedUpdateNetwork"/>.
    /// </summary>
    protected virtual void Startup() {
      
      // Startup Fix for shared mode incompatible configuration.
      if (_physicsAuthority == PhysicsAuthorities.Fusion 
          && _physicsTiming == PhysicsTimings.FixedUpdateNetwork 
          && ClientPhysicsSimulation == ClientPhysicsSimulation.Disabled 
          && Runner.GameMode == GameMode.Shared)
      {

        var targetAuthority = Runner.Config.PeerMode == NetworkProjectConfig.PeerModes.Single
          ? PhysicsAuthorities.Unity
          : PhysicsAuthorities.Auto;
        
        Log.Warn($"Incompatible configuration on {GetType().Name} for shared mode. " +
                 $"Current physics authority: {_physicsAuthority} will be changed to {targetAuthority}. " +
                 $"Current physics timing: {_physicsTiming} will be changed to {PhysicsTimings.FixedUpdate}. " +
                 $"Current client physics simulation: {ClientPhysicsSimulation} will be changed to {ClientPhysicsSimulation.SimulateAlways}.");
        _physicsAuthority = targetAuthority; 
        _physicsTiming = PhysicsTimings.FixedUpdate;
        ClientPhysicsSimulation = ClientPhysicsSimulation.SimulateAlways;
      }
      
      // Resolve 'Auto" to give Unity or Fusion control of Physics.Simulate
      // Should let Unity handle Physics if running Single-Peer, and in a valid Timing that Unity can Handle.
      _physicsAuthority = _physicsAuthority == PhysicsAuthorities.Auto ?
        Runner.Config.PeerMode == NetworkProjectConfig.PeerModes.Single && (Runner.GameMode == GameMode.Shared || Runner.Mode == SimulationModes.Host) && _physicsTiming != PhysicsTimings.FixedUpdateNetwork ? PhysicsAuthorities.Unity : PhysicsAuthorities.Fusion :
        _physicsAuthority;

#if UNITY_EDITOR
      if (_physicsAuthority == PhysicsAuthorities.Unity && Runner.Config.PeerMode == NetworkProjectConfig.PeerModes.Multiple) {
        Debug.LogWarning($"{GetType().Name}.{nameof(_physicsAuthority)} setting is forcing Unity as the Physics Authority. However in Multi-Peer Mode your Physics Scenes will not simulate. Set to Auto.");
      }
#endif

      _enabledRunnersCount++;

      // When the first Runner becomes active, determine if Unity or Fusion should be Simulating Physics, and cache the previous setting for shutdown restore
      if (_enabledRunnersCount == 1) {
        OverrideAutoSimulate(_physicsAuthority == PhysicsAuthorities.Unity);
      }

      // If we ended up letting Unity run physics, make sure FixedUpdate's interval matches Fusion's
      if (SetUnityFixedTimestep) {
        Time.fixedDeltaTime = Runner.DeltaTime;
      }

      _isInitialized = true;
    }

    /// <summary>
    /// Shutdown code executed when associated <see cref="NetworkRunner"/> shuts down.
    /// </summary>
    protected virtual void Shutdown() {
      if (_isInitialized == false)
        return;

      _isInitialized = false;

      _enabledRunnersCount--;

      // When the last Runner shuts down, restore Physics.AutoSimulate
      if (PhysicsAuthority == PhysicsAuthorities.Fusion && _enabledRunnersCount == 0) {
        RestoreAutoSimulate();
      }
    }

    private void Update() {

      if (_isInitialized == false) { return; }

      // If selected timing is not Update, this Update callback should be ignored
      if (_physicsTiming != PhysicsTimings.Update) { return; }

      // if Unity is currently auto-simulating - Fusion should not.
      if (UnityPhysicsMode != PhysicsTimings.Script) { return; }

      var deltaTime = Time.deltaTime * DeltaTimeMultiplier;
      // Debug.LogWarning($"Update Sim {deltaTime}");
      SimulationExecute(deltaTime, false);
    }

    private void OnDestroy()
    {
      Shutdown();
    }

    /// <summary>
    /// Unity FixedUpdate callback.
    /// </summary>
    public void FixedUpdate() {

      if (_isInitialized == false) { return; }

      if (_physicsTiming == PhysicsTimings.FixedUpdate) {
        // For some reason this needs to be reapplied.
        if (SetUnityFixedTimestep) {
          Time.fixedDeltaTime = Runner.DeltaTime;
        }
      } else {
        // The selected timing is not FixedUpdate, this FixedUpdate callback should be ignored
        return;
      }

      // if Unity is currently auto-simulating - Fusion should not.
      if (UnityPhysicsMode != PhysicsTimings.Script) { return; }

      var deltaTime = Time.fixedDeltaTime * DeltaTimeMultiplier;
      SimulationExecute(deltaTime, false);
    }

    /// <inheritdoc/>
    public override void FixedUpdateNetwork() {
      // We have no Spawned(), so initializing on first FUN
      if (_isInitialized == false) {
        Startup();
      }

      // We have no Despawned(), so testing for shutdown here.
      if (Runner.IsShutdown) {
        Shutdown();
        return;
      }

      // Currently getting physics info for both Shared and Server Modes
      if (Runner.TryGetPhysicsInfo(out NetworkPhysicsInfo info)) {
        if (Runner.IsServer || Runner.IsSharedModeMasterClient) {
          info.TimeScale = DeltaTimeMultiplier;
          Runner.TrySetPhysicsInfo(info);
        } else {
          DeltaTimeMultiplier = info.TimeScale;
        }
      }

      // If selected timing is not FixedUpdateNetwork, this FixedUpdateNetwork callback should be ignored
      if (_physicsTiming != PhysicsTimings.FixedUpdateNetwork) { return; }

      // if Unity is currently auto-simulating - Fusion should not.
      if (UnityPhysicsMode != PhysicsTimings.Script) { return; }

      var  deltaTime = PhysicsSimulationDeltaTime * DeltaTimeMultiplier;

      SimulationExecute(deltaTime, true);
    }

    private void SimulationExecute(float deltaTime, bool checkPhysicsSimulation) {

      if (DeltaTimeMultiplier <= 0) {
        return;
      }

      if (checkPhysicsSimulation) {
        if (CanSimulatePhysics(ClientPhysicsSimulation)) {
          DoSimulatePrimaryScene(deltaTime);
        } else if (RequiresSyncTransform(ClientPhysicsSimulation) || AnySceneRequiresSyncTransform()) {
          UnityEngine.Physics.SyncTransforms();
        }
      } else {
        DoSimulatePrimaryScene(deltaTime);
      }

      SimulateAdditionalScenes(deltaTime, checkPhysicsSimulation);
    }

    void IBeforeTick.BeforeTick() {
      HasSimulatedThisTick = false;
    }

    protected bool CanSimulatePhysics(ClientPhysicsSimulation clientPhysicsSimulation) {
      if (Runner.IsServer) {
        return true;
      }

      return clientPhysicsSimulation == ClientPhysicsSimulation.SimulateAlways || (clientPhysicsSimulation == ClientPhysicsSimulation.SimulateForward && Runner.IsForward);
    }

    protected bool RequiresSyncTransform(ClientPhysicsSimulation clientPhysicsSimulation) {
      return clientPhysicsSimulation == ClientPhysicsSimulation.SyncTransforms || (clientPhysicsSimulation == ClientPhysicsSimulation.SimulateForward && !Runner.IsForward);
    }

    /// <summary>
    /// Executes the simulation of the primary physics scene and triggers the associated callback interfaces.
    /// </summary>
    protected virtual void DoSimulatePrimaryScene(float deltaTime) {

      while (_onBeforeSimulateCallbacks.Count > 0) {
        _onBeforeSimulateCallbacks.Dequeue().Invoke();
      }
      OnBeforeSimulate?.Invoke();
      SimulatePrimaryScene(deltaTime);
      HasSimulatedThisTick = true;

      while (_onAfterSimulateCallbacks.Count > 0) {
        _onAfterSimulateCallbacks.Dequeue().Invoke();
      }
      OnAfterSimulate?.Invoke();
    }

    protected virtual bool AnySceneRequiresSyncTransform() => false;
  }
}
