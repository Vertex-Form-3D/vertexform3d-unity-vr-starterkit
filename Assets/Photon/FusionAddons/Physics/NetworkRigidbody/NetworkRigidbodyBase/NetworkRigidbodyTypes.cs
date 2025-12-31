using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Fusion.Addons.Physics {

  /// <summary>
  /// Extended <see cref="NetworkTRSPData"/> struct, with additional fields specific to Rigidbody synchronization.
  /// The first field is a <see cref="NetworkTRSPData"/>, which ensures that Position/Rotation/Scale/Parent data
  /// all are in the location within the struct expected by the  <see cref="NetworkTRSP"/> base class.
  /// </summary>
  [StructLayout(LayoutKind.Explicit)]
  public struct NetworkRBData : INetworkStruct {

    /// <summary>
    /// The number of words used by this struct.
    /// </summary>
    public const int WORDS = NetworkTRSPData.WORDS + 24;
    /// <summary>
    /// The number of bytes used by this struct
    /// </summary>
    public const int SIZE  = WORDS * Allocator.REPLICATE_WORD_SIZE;

    /// <summary>
    /// The required Translate/Rotation/Scale/Parent struct used by the NetworkTRSP base class.
    /// This places transform data in the first memory positions of NetworkRigidbody struct.
    /// Of specific importance here is the first word for Translate (Position), as position is used by
    /// Area Of Interest for interest determinations.
    /// </summary>
    [FieldOffset(0)]
    public NetworkTRSPData TRSPData;

    /// <summary>
    /// Word used to store rigidbody drag value.
    /// </summary>
    [FieldOffset((NetworkTRSPData.WORDS + 0) * Allocator.REPLICATE_WORD_SIZE)]
    public FloatCompressed Drag;

    /// <summary>
    /// Word used to store rigidbody angular drag value.
    /// </summary>
    [FieldOffset((NetworkTRSPData.WORDS + 1) * Allocator.REPLICATE_WORD_SIZE)]
    public FloatCompressed AngularDrag;

    /// <summary>
    /// Word used to store rigidbody mass value.
    /// </summary>
    [FieldOffset((NetworkTRSPData.WORDS + 2) * Allocator.REPLICATE_WORD_SIZE)]
    public FloatCompressed Mass;

    /// <summary>
    /// Backing field used to store additional rigidbody bool and enum values, encoded into a single word.
    /// </summary>
    [FieldOffset((NetworkTRSPData.WORDS + 3) * Allocator.REPLICATE_WORD_SIZE)]
    int _flags;

    /// <summary>
    /// Property for getting and setting additional rigidbody data in this struct.
    /// </summary>
    public (NetworkRigidbodyFlags flags, int constraints) Flags {
      get {
        var f = (NetworkRigidbodyFlags)((_flags) & 0xFF);
        var c = (int)((_flags >> 8) & 0xFF);
        return (f, c);
      }
      set {
        var (f, c) = value;
        _flags =  (int)f;
        _flags |= (int)c << 8;
      }
    }

    // 3D

    /// <summary>
    /// Words used to store rigidbody velocity value.
    /// </summary>
    [FieldOffset((NetworkTRSPData.WORDS + 4) * Allocator.REPLICATE_WORD_SIZE)]
    public Vector3Compressed LinearVelocity;

    /// <summary>
    /// Words used to store rigidbody angular velocity value.
    /// </summary>
    [FieldOffset((NetworkTRSPData.WORDS + 7) * Allocator.REPLICATE_WORD_SIZE)]
    public Vector3Compressed AngularVelocity;

    /// <summary>
    /// Property used to store rigidbody2D velocity value.
    /// Uses the <see cref="LinearVelocity"/> field, and just casts to and from Vector2.
    /// </summary>
    public Vector2 LinearVelocity2D {
      get => LinearVelocity;
      set => LinearVelocity = value;
    }

    /// <summary>
    /// Property used to store rigidbody2D angular velocity value.
    /// Uses the <see cref="AngularVelocity"/> field, and just casts to and from Vector2.
    /// </summary>
    public float AngularVelocity2D {
      get => AngularVelocity.Z;
      set => AngularVelocity.Z = value;
    }

    // 2D

    /// <summary>
    /// Property used to store rigidbody2D gravity scale value.
    /// Uses the z axis of the <see cref="LinearVelocity"/> field, to store the Rigidbody2D gravity scale.
    /// </summary>
    public float GravityScale2D {
      get => LinearVelocity.Z;
      set => LinearVelocity.Z = value;
    }

    // Sleep states

    /// <summary>
    /// Word used to store uncompressed position value. This is typically only changed and used when the object
    /// goes to sleep, to ensure that the final resting state is complete agreement with the state authority simulation
    /// result.
    /// </summary>
    [FieldOffset((NetworkTRSPData.WORDS + 10) * Allocator.REPLICATE_WORD_SIZE)]
    public Vector3 FullPrecisionPosition;

    /// <summary>
    /// Word used to store uncompressed rotation value. This is typically only changed and used when the object
    /// goes to sleep, to ensure that the final resting state is complete agreement with the state authority simulation
    /// result.
    /// </summary>
    [FieldOffset((NetworkTRSPData.WORDS + 13) * Allocator.REPLICATE_WORD_SIZE)]
    public Quaternion FullPrecisionRotation;

    /// <summary>
    /// Word used to store teleport position "To" value. This is used for moving teleports, and is the value used for
    /// interpolating into a teleport.
    /// </summary>
    [FieldOffset((NetworkTRSPData.WORDS + 17) * Allocator.REPLICATE_WORD_SIZE)]
    public Vector3Compressed TeleportPosition;
    /// <summary>
    /// Word used to store teleport rotation "To" value. This is used for moving teleports, and is the value used for
    /// interpolating into a teleport.
    /// </summary>
    [FieldOffset((NetworkTRSPData.WORDS + 20) * Allocator.REPLICATE_WORD_SIZE)]
    public QuaternionCompressed TeleportRotation;

  }

  /// <summary>
  /// Networked flags representing a 2D or 3D rigid body state and characteristics.
  /// </summary>
  [Flags]
  public enum NetworkRigidbodyFlags : byte {
    /// <summary>
    /// Networked kinematic state.
    /// See also <see cref="Rigidbody.isKinematic"/> or <see cref="Rigidbody2D.isKinematic"/>.
    /// </summary>
    IsKinematic = 1 << 0,

    /// <summary>
    /// Networked sleeping state.
    /// See also <see cref="Rigidbody.IsSleeping"/> or <see cref="Rigidbody2D.IsSleeping"/>.
    /// </summary>
    IsSleeping = 1 << 1,

    /// <summary>
    /// Networked <see cref="Rigidbody.useGravity"/> state. Not used with 2D rigid bodies.
    /// </summary>
    UseGravity = 1 << 2,
  }

  [Serializable]
  public struct TRSThresholds {
    /// <summary>
    /// If enabled, the energy value of the networked state will be used to determine if interpolation will be applied.
    /// Only applicable when there is no Interpolation Target set.
    /// </summary>
    [InlineHelp]
    public bool UseEnergy;
    /// <summary>
    /// The Magnitude of the difference between the current position of the Rigidbody and interpolated position.
    /// If the Magnitude of the difference is less than this value, then the Rigidbody will not be changed during Render,
    /// allowing the Rigidbody to sleep, thus retaining cached friction states.
    /// A value of 0 indicates that this threshold should not be factored in to determining if interpolation occurs.
    /// </summary>
    [InlineHelp]
    [Unit(Units.Units)]
    public float Position;
    /// <summary>
    /// The minimum Quanternion.Angle difference between the current rotation angle of the Rigidbody and interpolated rotation, for interpolation to be applied.
    /// If the angle between current and interpolated values are less than this, then the transform will not be moved for interpolation,
    /// allowing the Rigidbody to sleep, thus retaining cached friction states.
    /// A value of 0 indicates that this threshold should not be factored in to determining if interpolation occurs.
    /// </summary>
    [InlineHelp]
    [Unit(Units.Degrees)]
    public float Rotation;
    /// <summary>
    /// The Magnitude of the difference between the current localScale of the Rigidbody and interpolated localScale.
    /// If the Magnitude of the difference is less than this value, then the Rigidbody will not be changed during Render,
    /// allowing the Rigidbody to sleep, thus retaining cached friction states.
    /// A value of 0 indicates that this threshold should not be factored in to determining if interpolation occurs.
    /// </summary>
    [InlineHelp]
    [Unit(Units.NormalizedPercentage)]
    public float Scale;

    /// <summary>
    /// The default values for interpolation threshold tests.
    /// </summary>
    public static TRSThresholds Default =>new TRSThresholds() {
      UseEnergy = true,
      Position = 0.01f,
      Rotation = 0.01f,
      Scale    = 0.01f
    };
  }
}
