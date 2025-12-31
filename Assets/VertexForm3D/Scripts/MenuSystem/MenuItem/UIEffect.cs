using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    TMP_InputField inputfield;
    TextMeshProUGUI textButton;
    public bool IsTextButton;
    public Vector3 scale = (Vector3.one * 1.3f);
    public UnityEvent onPointerEnterEvent;
    public UnityEvent onPointerExitEvent;
    void Start()
    {
        if (GetComponent<TMP_InputField>() != null)
        {
            inputfield = GetComponent<TMP_InputField>();
            inputfield.onSelect.AddListener(OnSelectInputField);
            inputfield.onSubmit.AddListener(OnSubmitInputField);
            inputfield.onDeselect.AddListener(OnDeselectInputField);
        }
        IsTextButton = GetComponent<TMP_Text>() != null;
        if (UIEffectManager.Instance != null)
        {
            GetPerchantageScale(UIEffectManager.Instance.percentageOfScaling);
        }
    }

    void GetPerchantageScale(float percentage)
    {
        // Get the current scale
        Vector3 currentScale = transform.localScale;

        // Calculate the scale multiplier (e.g., 10% increase = 1 + 10/100 = 1.1)
        float scaleMultiplier = 1 + percentage / 100;

        // Apply the percentage increase to each axis
        Vector3 newScale = new Vector3(
            currentScale.x * scaleMultiplier,
            currentScale.y * scaleMultiplier,
            currentScale.z * scaleMultiplier
        );

        // Set the new scale
        scale = newScale;
    }

    public void OnSelectInputField(string s)
    {
        UIEffectManager.Instance.IsInputFieldSelected = true;
        gameObject.transform.DOScale(scale, .5f);
    }
    public void OnSubmitInputField(string s)
    {
        EventSystem.current.SetSelectedGameObject(null);
    }
    public void OnDeselectInputField(string s)
    {
        gameObject.transform.DOScale(Vector3.one, .5f);
        UIEffectManager.Instance.IsInputFieldSelected = false;
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UIEffectManager.Instance != null && !UIEffectManager.Instance.IsInputFieldSelected)
        {
            gameObject.transform.DOScale(scale, .5f);
            if (IsTextButton)
            {
                textButton.fontStyle = FontStyles.Underline;
            }
            if (onPointerEnterEvent != null)
            {
                onPointerEnterEvent.Invoke();
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (UIEffectManager.Instance != null && !UIEffectManager.Instance.IsInputFieldSelected)
        {
            gameObject.transform.DOScale(Vector3.one, .5f);
            if (IsTextButton)
            {
                textButton.fontStyle = FontStyles.Normal;
            }
            if (onPointerExitEvent != null)
            {
                onPointerExitEvent.Invoke();
            }
        }
    }
}
