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
using System.Collections;

namespace VRKeys {

	/// <summary>
	/// Shift key toggles between lower and upper case and symbols.
	/// </summary>
	public class ShiftKey : Key {
		private bool shifted = false;

		public override void HandleTriggerEnter (Collider other) {
			keyboard.ToggleShift ();
		}

		public override void Enable () {
			base.Enable ();

			meshRenderer.material = shifted ? activeMat : inactiveMat;
		}

		public void Toggle (bool shift) {
			shifted = shift;

			meshRenderer.material = shifted ? activeMat : inactiveMat;
		}

		public override void UpdateLayout (Layout translation) {
			label.text = translation.shiftButtonLabel;
		}
	}
}