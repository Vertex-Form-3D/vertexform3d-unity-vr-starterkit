/**
 * Copyright (c) 2017 The Campfire Union Inc - All Rights Reserved.
 *
 * Licensed under the MIT license. See LICENSE file in the project root for
 * full license information.
 *
 * Email:   info@campfireunion.com
 * Website: https://www.campfireunion.com
 */

using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace VRKeys {

	/// <summary>
	/// Base class for platform-specific inputs and controller access.
	/// </summary>
	public class Controller : MonoBehaviour {
		private Mallet mallet;
		private InputDevice _device = new InputDevice ();

		[Tooltip("Input Device Characteristics")]
		public InputDeviceCharacteristics controllerCharacteristics;

		private void Start () {
			mallet = GetComponent<Mallet> ();
			//controllerCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
			controllerCharacteristics = (mallet.hand == Mallet.MalletHand.Left) ? InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand : InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand;
		}

		private InputDevice GetDevice () {
			if (_device.isValid) return _device;
			//if (role == InputDeviceRole.Unknown) return _device;

			List<InputDevice> devices = new List<InputDevice> ();
			InputDevices.GetDevicesWithCharacteristics(controllerCharacteristics,devices);
			if (devices.Count > 0 && devices[0].isValid) {
				_device = devices[0];
			}

			return _device;
		}

		private bool DeviceIsValid () {
			return GetDevice ().isValid;
		}

		public void TriggerPulse () {
			if (!DeviceIsValid ()) return;

			GetDevice ().SendHapticImpulse (0, 0.3f, 0.05f);
		}

		public bool OnGrip () {
			if (!DeviceIsValid ()) return false;

			bool value;
			GetDevice ().TryGetFeatureValue (CommonUsages.gripButton, out value);
			return value;
		}
	}
}