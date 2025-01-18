using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using VRKeys;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.EventSystems;
/// <summary>
/// Sets up the VR Keyboard and manages it
/// </summary>
public class VRKeyboardManager : MonoBehaviour
{
	/// <summary>
	/// Reference to the VRKeys keyboard.
	/// </summary>
	public Keyboard keyboard;


	public GameObject localVRPlayerCamera;


	public Vector3 relativePosition = new Vector3(0,1,2);

	public TMP_InputField playerNameInputField;

	public GameObject leftMarret;
	public GameObject rightMarret;


	public GameObject leftBaseController; 
	public GameObject rightBaseController;


	/// <summary>
	/// Show the keyboard with a custom input message. Attaching events dynamically,
	/// but you can also use the inspector.
	/// </summary>
	public void EnableVRKeyboard()
	{		
		keyboard.Enable();
		keyboard.SetPlaceholderMessage("What should we call you?");

		keyboard.OnUpdate.AddListener(HandleUpdate);
		keyboard.OnSubmit.AddListener(HandleSubmit);
		keyboard.OnCancel.AddListener(HandleCancel);

		keyboard.gameObject.transform.position = localVRPlayerCamera.transform.position + relativePosition;
		AttachMarrets();

		//leftBaseController.GetComponent<XRRayInteractor>().enabled = false;
		//rightBaseController.GetComponent<XRRayInteractor>().enabled = false;

	}

	void AttachMarrets()
	{
		leftMarret.transform.SetParent(leftBaseController.transform);
		leftMarret.transform.localPosition = Vector3.zero;
		leftMarret.transform.localRotation = Quaternion.Euler(new Vector3(90f,0f,0f));
		leftMarret.SetActive(true);

		rightMarret.transform.SetParent(rightBaseController.transform);
		rightMarret.transform.localPosition = Vector3.zero;
		rightMarret.transform.localRotation = Quaternion.Euler(new Vector3(90f, 0f, 0f));
		rightMarret.SetActive(true);
	}

	void DetachMarrets()
	{
		leftMarret.transform.SetParent(null);
		leftMarret.SetActive(false);

		rightMarret.transform.SetParent(null);
		rightMarret.SetActive(false);

	}

	public void DisableVRKeyboard() 
	{
		keyboard.OnUpdate.RemoveListener(HandleUpdate);
		keyboard.OnSubmit.RemoveListener(HandleSubmit);
		keyboard.OnCancel.RemoveListener(HandleCancel);

		keyboard.Disable();

		DetachMarrets();

	  //  leftBaseController.GetComponent<XRRayInteractor>().enabled = true;
	//	rightBaseController.GetComponent<XRRayInteractor>().enabled = true;


	}

	/// <summary>
	/// Press space to show/hide the keyboard.
	///
	/// Press Q for Qwerty keyboard, D for Dvorak keyboard, and F for French keyboard.
	/// </summary>
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			if (keyboard.disabled)
			{
				EnableVRKeyboard();
			}
			else
			{
				DisableVRKeyboard();
			}
		}

		if (keyboard.disabled)
		{
			return;
		}		
	}

	/// <summary>
	/// Hide the validation message on update. Connect this to OnUpdate.
	/// </summary>
	public void HandleUpdate(string text)
	{
		keyboard.HideValidationMessage();
		playerNameInputField.text = text;
		playerNameInputField.caretPosition = playerNameInputField.text.Length;

	}

	/// <summary>
	/// Connect this to OnSubmit.
	/// </summary>
	public void HandleSubmit(string text)
	{
		DisableVRKeyboard();
		//playerNameInputField.OnDeselect();

		var eventSystem = EventSystem.current;
		if (!eventSystem.alreadySelecting) eventSystem.SetSelectedGameObject(null);
	}

	public void HandleCancel()
	{
		Debug.Log("Cancelled keyboard input!");
		DisableVRKeyboard();

		var eventSystem = EventSystem.current;
		if (!eventSystem.alreadySelecting) eventSystem.SetSelectedGameObject(null);
	}

	public void OnSelectEnter()
	{
		Debug.Log("Entered");
	}

	public void OnSelectExit()
	{
		Debug.Log("Exited");

	}
}
