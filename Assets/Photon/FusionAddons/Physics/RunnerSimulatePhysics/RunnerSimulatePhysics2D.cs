using UnityEngine;

namespace Fusion.Addons.Physics {
  /// <summary>
  /// Fusion component for handling Physics2D.Simulate().
  /// </summary>
  [DisallowMultipleComponent]
  public class RunnerSimulatePhysics2D : RunnerSimulatePhysicsBase<PhysicsScene2D> {

    /// <inheritdoc/>
    protected override void OverrideAutoSimulate(bool set) {
      _physicsAutoSimRestore = (PhysicsTimings)Physics2D.simulationMode;
      if (set) {
        Physics2D.simulationMode = (SimulationMode2D)_physicsTiming;
      } else {
        Physics2D.simulationMode = SimulationMode2D.Script;
      }
    }

    /// <inheritdoc/>
    protected override void RestoreAutoSimulate() {
      Physics2D.simulationMode = (SimulationMode2D)_physicsAutoSimRestore;
    }

    /// <inheritdoc/>
    protected override bool AutoSyncTransforms {
      get => Physics2D.autoSyncTransforms;
      set => Physics2D.autoSyncTransforms = value;
    }

    /// <inheritdoc/>
    protected override PhysicsTimings UnityPhysicsMode => (PhysicsTimings)Physics2D.simulationMode;

    /// <inheritdoc/>
    protected override void SimulatePrimaryScene(float deltaTime) {
      if (Runner.SceneManager.TryGetPhysicsScene2D(out var physicsScene)) {
        if (physicsScene.IsValid()) {
          physicsScene.Simulate(deltaTime);
        } else {
          Physics2D.Simulate(deltaTime);
        }
      }
    }

    /// <inheritdoc/>
    protected override void SimulateAdditionalScenes(float deltaTime, bool checkPhysicsSimulation) {
      if (_additionalScenes == null || _additionalScenes.Count == 0) {
        return;
      }
      var defaultPhysicsScene = Physics2D.defaultPhysicsScene;
      foreach (var scene in _additionalScenes) {
        if (!checkPhysicsSimulation || CanSimulatePhysics(scene.ClientPhysicsSimulation)) {
          if (scene.PhysicsScene != defaultPhysicsScene || Physics2D.simulationMode == SimulationMode2D.Script) {
            scene.PhysicsScene.Simulate(deltaTime);
          }
        }
      }
    }
  }
}
