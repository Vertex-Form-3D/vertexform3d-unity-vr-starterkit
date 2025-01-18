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
using UnityEngine.Events;
using System;
using System.Collections;
using TMPro;

namespace VRKeys {

	/// <summary>
	/// Keyboard input system for use with NewtonVR. To use, drop the VRKeys prefab
	/// into your scene and activate as needed. Listen for OnUpdate and OnSubmit events,
	/// and set the text via SetText(string).
	///
	/// Input validation can be done during OnUpdate and OnSubmit events by calling
	/// ShowValidationMessage(msg) and HideValidationMessage(). The keyboard does not
	/// automatically hide OnSubmit, but rather you should call SetActive(false) when
	/// you have finished validating the submitted text.
	/// </summary>
	public class Keyboard : MonoBehaviour {
		public Vector3 positionRelativeToUser = new Vector3 (0f, 1.35f, 2f);

		public KeyboardLayout keyboardLayout = KeyboardLayout.Qwerty;

		[Space (15)]
		public TextMeshProUGUI placeholder;

		public string placeholderMessage = "Tap the keys to begin typing";

		public TextMeshProUGUI displayText;

		public GameObject validationNotice;

		public TextMeshProUGUI validationMessage;

		public GameObject infoNotice;

		public TextMeshProUGUI infoMessage;

		public GameObject successNotice;

		public TextMeshProUGUI successMessage;

		[Space (15)]
		public Color displayTextColor = Color.black;

		public Color caretColor = Color.gray;

		[Space (15)]
		public GameObject keyPrefab;

		public Transform keysParent;

		public float keyWidth = 0.16f;

		public float keyHeight = 0.16f;

		[Space (15)]
		public string text = "";

		[Space (15)]
		public GameObject canvas;

		public GameObject leftMallet;

		public GameObject rightMallet;

		public GameObject keyboardWrapper;

		public ShiftKey shiftKey;

		public Key[] extraKeys;

		[Space (15)]
		public bool leftPressing = false;

		public bool rightPressing = false;

		public bool initialized = false;

		public bool disabled = true;

		[Serializable]
		public class KeyboardUpdateEvent : UnityEvent<string> { }

		[Serializable]
		public class KeyboardSubmitEvent : UnityEvent<string> { }

		[Space (15)]

		/// <summary>
		/// Listen for events whenever the text changes.
		/// </summary>
		public KeyboardUpdateEvent OnUpdate = new KeyboardUpdateEvent ();

		/// <summary>
		/// Listen for events when Submit() is called.
		/// </summary>
		public KeyboardSubmitEvent OnSubmit = new KeyboardSubmitEvent ();

		/// <summary>
		/// Listen for events when Cancel() is called.
		/// </summary>
		public UnityEvent OnCancel = new UnityEvent ();

		private GameObject playerSpace;

		private GameObject leftHand;

		private GameObject rightHand;

		private LetterKey[] keys;

		private bool shifted = false;

		private Layout layout;

		/// <summary>
		/// Initialization.
		/// </summary>
		private IEnumerator Start () {
			

			yield return StartCoroutine (DoSetLanguage (keyboardLayout));

			validationNotice.SetActive (false);
			infoNotice.SetActive (false);
			successNotice.SetActive (false);

			UpdateDisplayText ();
			PlaceholderVisibility ();

			initialized = true;
		}

		

		

		

		/// <summary>
		/// Make sure mallets don't stay attached if VRKeys is disabled without
		/// calling Disable().
		/// </summary>
		private void OnDisable () {
			Disable ();
		}

		/// <summary>
		/// Enable the keyboard.
		/// </summary>
		public void Enable () {
			if (!initialized) {
				// Make sure we're initialized first.
				StartCoroutine (EnableWhenInitialized ());
				return;
			}

			disabled = false;

			if (canvas != null) {
				canvas.SetActive (true);
			}

			if (keysParent != null) {
				keysParent.gameObject.SetActive (true);
			}

			EnableInput ();

			
		}

		private IEnumerator EnableWhenInitialized () {
			yield return new WaitUntil (() => initialized);

			Enable ();
		}

		/// <summary>
		/// Disable the keyboard.
		/// </summary>
		public void Disable () {
			disabled = true;

			if (canvas != null) {
				canvas.SetActive (false);
			}

			if (keysParent != null) {
				keysParent.gameObject.SetActive (false);
			}

			
		}

		/// <summary>
		/// Set the text value all at once.
		/// </summary>
		/// <param name="txt">New text value.</param>
		public void SetText (string txt) {
			text = txt;

			UpdateDisplayText ();
			PlaceholderVisibility ();

			OnUpdate.Invoke (text);
		}

		/// <summary>
		/// Add a character to the input text.
		/// </summary>
		/// <param name="character">Character.</param>
		public void AddCharacter (string character) {
			text += character;

			UpdateDisplayText ();
			PlaceholderVisibility ();

			OnUpdate.Invoke (text);

			if (shifted && character != "" && character != " ") {
				StartCoroutine (DelayToggleShift ());
			}
		}

		/// <summary>
		/// Toggle whether the characters are shifted (caps).
		/// </summary>
		public bool ToggleShift () {
			if (keys == null) {
				return false;
			}

			shifted = !shifted;

			foreach (LetterKey key in keys) {
				key.shifted = shifted;
			}

			shiftKey.Toggle (shifted);

			return shifted;
		}

		private IEnumerator DelayToggleShift () {
			yield return new WaitForSeconds (0.1f);

			ToggleShift ();
		}

		/// <summary>
		/// Disable keyboard input.
		/// </summary>
		public void DisableInput () {
			leftPressing = false;
			rightPressing = false;

			if (keys != null) {
				foreach (LetterKey key in keys) {
					if (key != null) {
						key.Disable ();
					}
				}
			}

			foreach (Key key in extraKeys) {
				key.Disable ();
			}
		}

		/// <summary>
		/// Re-enable keyboard input.
		/// </summary>
		public void EnableInput () {
			leftPressing = false;
			rightPressing = false;

			

			if (keys != null) {
				foreach (LetterKey key in keys) {
					if (key != null) {
						key.Enable ();
					}
				}
			}

			foreach (Key key in extraKeys) {
				key.Enable ();
			}
		}

		/// <summary>
		/// Backspace one character.
		/// </summary>
		public void Backspace () {
			if (text.Length > 0) {
				text = text.Substring (0, text.Length - 1);
			}

			UpdateDisplayText ();
			PlaceholderVisibility ();

			OnUpdate.Invoke (text);
		}

		/// <summary>
		/// Submit and close the keyboard.
		/// </summary>
		public void Submit () {
			OnSubmit.Invoke (text);
		}

		/// <summary>
		/// Cancel input and close the keyboard.
		/// </summary>
		public void Cancel () {
			OnCancel.Invoke ();
			Disable ();
		}

		/// <summary>
		/// Set the language of the keyboard.
		/// </summary>
		/// <param name="layout">New language.</param>
		public void SetLayout (KeyboardLayout layout) {
			StartCoroutine (DoSetLanguage (layout));
		}

		private IEnumerator DoSetLanguage (KeyboardLayout lang) {
			keyboardLayout = lang;
			layout = LayoutList.GetLayout (keyboardLayout);

			placeholderMessage = layout.placeholderMessage;

			yield return StartCoroutine (SetupKeys ());

			// Update extra keys
			foreach (Key key in extraKeys) {
				key.UpdateLayout (layout);
			}
		}

		/// <summary>
		/// Set a custom placeholder message.
		/// </summary>
		/// <param name="msg">Message.</param>
		public void SetPlaceholderMessage (string msg) {
			StartCoroutine (DoSetPlaceholderMessage (msg));
		}

		private IEnumerator DoSetPlaceholderMessage (string msg) {
			if (!initialized) {
				yield return new WaitUntil (() => initialized);
			}

			placeholder.text = placeholderMessage = msg;

			yield break;
		}

		/// <summary>
		/// Show the specified validation notice.
		/// </summary>
		/// <param name="message">Message to show.</param>
		public void ShowValidationMessage (string message) {
			validationMessage.text = message;
			validationNotice.SetActive (true);
			infoNotice.SetActive (false);
			successNotice.SetActive (false);
		}

		/// <summary>
		/// Show the specified input notice.
		/// </summary>
		/// <param name="message">Message to show.</param>
		public void ShowInfoMessage (string message) {
			infoMessage.text = message;
			validationNotice.SetActive (false);
			infoNotice.SetActive (true);
			successNotice.SetActive (false);
		}

		/// <summary>
		/// Show the specified success notice.
		/// </summary>
		/// <param name="message">Message to show.</param>
		public void ShowSuccessMessage (string message) {
			successMessage.text = message;
			validationNotice.SetActive (false);
			infoNotice.SetActive (false);
			successNotice.SetActive (true);
		}

		/// <summary>
		/// Hide the validation notice.
		/// </summary>
		public void HideValidationMessage () {
			validationNotice.SetActive (false);
		}

		/// <summary>
		/// Hide the info notice.
		/// </summary>
		public void HideInfoMessage () {
			infoNotice.SetActive (false);
		}

		/// <summary>
		/// Hide the success notice.
		/// </summary>
		public void HideSuccessMessage () {
			successNotice.SetActive (false);
		}

		/// <summary>
		/// Setup the keys.
		/// </summary>
		private IEnumerator SetupKeys () {
			bool activeState = canvas.activeSelf;

			// Hide everything before setting up the keys
			canvas.SetActive (false);
			keysParent.gameObject.SetActive (false);

			// Remove previous keys
			if (keys != null) {
				foreach (Key key in keys) {
					if (key != null) {
						Destroy (key.gameObject);
					}
				}
			}

			keys = new LetterKey[layout.TotalKeys ()];
			int keyCount = 0;

			// Numbers row
			for (int i = 0; i < layout.row1Keys.Length; i++) {
				GameObject obj = (GameObject) Instantiate (keyPrefab, keysParent);
				obj.transform.localPosition += (Vector3.right * ((keyWidth * i) - layout.row1Offset));

				LetterKey key = obj.GetComponent<LetterKey> ();
				key.character = layout.row1Keys[i];
				key.shiftedChar = layout.row1Shift[i];
				key.shifted = false;
				key.Init (obj.transform.localPosition);

				obj.name = "Key: " + layout.row1Keys[i];
				obj.SetActive (true);

				keys[keyCount] = key;
				keyCount++;

				yield return null;
			}

			// QWERTY row
			for (int i = 0; i < layout.row2Keys.Length; i++) {
				GameObject obj = (GameObject) Instantiate (keyPrefab, keysParent);
				obj.transform.localPosition += (Vector3.right * ((keyWidth * i) - layout.row2Offset));
				obj.transform.localPosition += (Vector3.back * keyHeight * 1);

				LetterKey key = obj.GetComponent<LetterKey> ();
				key.character = layout.row2Keys[i];
				key.shiftedChar = layout.row2Shift[i];
				key.shifted = false;
				key.Init (obj.transform.localPosition);

				obj.name = "Key: " + layout.row2Keys[i];
				obj.SetActive (true);

				keys[keyCount] = key;
				keyCount++;

				yield return null;
			}

			// ASDF row
			for (int i = 0; i < layout.row3Keys.Length; i++) {
				GameObject obj = (GameObject) Instantiate (keyPrefab, keysParent);
				obj.transform.localPosition += (Vector3.right * ((keyWidth * i) - layout.row3Offset));
				obj.transform.localPosition += (Vector3.back * keyHeight * 2);

				LetterKey key = obj.GetComponent<LetterKey> ();
				key.character = layout.row3Keys[i];
				key.shiftedChar = layout.row3Shift[i];
				key.shifted = false;
				key.Init (obj.transform.localPosition);

				obj.name = "Key: " + layout.row3Keys[i];
				obj.SetActive (true);

				keys[keyCount] = key;
				keyCount++;

				yield return null;
			}

			// ZXCV row
			for (int i = 0; i < layout.row4Keys.Length; i++) {
				GameObject obj = (GameObject) Instantiate (keyPrefab, keysParent);
				obj.transform.localPosition += (Vector3.right * ((keyWidth * i) - layout.row4Offset));
				obj.transform.localPosition += (Vector3.back * keyHeight * 3);

				LetterKey key = obj.GetComponent<LetterKey> ();
				key.character = layout.row4Keys[i];
				key.shiftedChar = layout.row4Shift[i];
				key.shifted = false;
				key.Init (obj.transform.localPosition);

				obj.name = "Key: " + layout.row4Keys[i];
				obj.SetActive (true);

				keys[keyCount] = key;
				keyCount++;

				yield return null;
			}

			// Reset visibility of canvas and keyboard
			canvas.SetActive (activeState);
			keysParent.gameObject.SetActive (activeState);
		}

		/// <summary>
		/// Update the display text, including trailing caret.
		/// </summary>
		private void UpdateDisplayText () {
			string display = (text.Length > 37) ? text.Substring (text.Length - 37) : text;

			displayText.text = string.Format (
				"<#{0}>{1}</color><#{2}>_</color>",
				ColorUtility.ToHtmlStringRGB (displayTextColor),
				display,
				ColorUtility.ToHtmlStringRGB (caretColor)
			);
		}

		/// <summary>
		/// Show/hide placeholder text.
		/// </summary>
		private void PlaceholderVisibility () {
			if (text == "") {
				placeholder.text = placeholderMessage;
				placeholder.gameObject.SetActive (true);
			} else {
				placeholder.gameObject.SetActive (false);
			}
		}
	}
}