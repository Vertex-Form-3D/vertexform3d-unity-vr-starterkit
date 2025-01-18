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

	public class HandCollider : MonoBehaviour {
		public Mallet mallet;

		public bool inKeyboard = false;

		private void OnTriggerEnter (Collider other) {
			if (other.gameObject.name == "KeyboardCollider") {
				inKeyboard = true;
			}
		}

		private void OnTriggerExit (Collider other) {
			if (other.gameObject.name == "KeyboardCollider") {
				inKeyboard = false;
			}
		}
	}
}