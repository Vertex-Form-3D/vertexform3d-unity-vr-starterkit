using System;
using UnityEngine;

namespace Fusion.Addons.Physics
{
  public partial class NetworkRigidbody<RBType, PhysicsSimType> {

    /// <summary>
    /// Holds teleport information for application after physics simulation.
    /// </summary>
    protected (Vector3? position, Quaternion? rotation, bool moving) _deferredTeleport;


    /// <inheritdoc/>
    public override void Teleport(Vector3? position = null, Quaternion? rotation = null) {
      if (Object.IsInSimulation == false) {
        return;
      }

      _deferredTeleport = (position, rotation, true);
      // for moving, be sure to apply AFTER simulation runs, we need to capture the sim results before teleporting.
      if (_physicsSimulator.HasSimulatedThisTick) {
        ApplyDeferredTeleport();
      } else {
        _physicsSimulator.QueueAfterSimulationCallback(ApplyDeferredTeleport);
      }
    }

    /// <summary>
    /// Called after Physics has simulated, and is where the resulting simulated RB state is captured for the teleport.
    /// </summary>
    protected virtual void ApplyDeferredTeleport() {
      bool moving = _deferredTeleport.moving;

      if (moving) {
        // For moving teleports this is happening after Physics.Simulate
        // So we can capture the results of the simulation before applying the teleport.
        Data.TeleportPosition = _transform.position;
        Data.TeleportRotation = _transform.rotation;
      }

      if (_deferredTeleport.position.HasValue) {
        _transform.position    = _deferredTeleport.position.Value;
        RBPosition             = _deferredTeleport.position.Value;
        Data.TRSPData.Position = _deferredTeleport.position.Value;
      }
      if (_deferredTeleport.rotation.HasValue) {
        _transform.rotation    = _deferredTeleport.rotation.Value;
        RBRotation             = _deferredTeleport.rotation.Value;

        if (UsePreciseRotation) {
          Data.FullPrecisionRotation = _deferredTeleport.rotation.Value;
        } else {
          Data.TRSPData.Rotation     = _deferredTeleport.rotation.Value;
        }
      }
      IncrementTeleportKey(moving);
    }

    /// <summary>
    /// Adds one to the current teleport key. Indicating the teleport as moving sets the sign to negative,
    /// as a flag to indicate that this teleport has a different To and From position/rotation target for the teleport tick.
    /// </summary>
    protected virtual void IncrementTeleportKey(bool moving) {
      // Keeping the key well under 1 byte in size
      var key = Math.Abs(Data.TRSPData.TeleportKey) + 1;
      if (key > 30) {
        key = 1;
      }
      // Positive indicates non-moving teleport, Negative indicates moving teleport
      Data.TRSPData.TeleportKey = moving ? -key : key;
    }

  }
}
