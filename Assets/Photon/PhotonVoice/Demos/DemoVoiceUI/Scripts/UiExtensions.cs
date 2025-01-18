namespace Photon.Voice.Unity.Demos
{
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;
    using Object = UnityEngine.Object;

    public static class UiExtensions
    {
        //http://answers.unity.com/answers/1610964/view.html
        //https://gist.github.com/Josef212/b3f4bcf9cd827f5dc125ffc013548491
        public static void SetPosX(this RectTransform rectTransform, float x)
        {
            rectTransform.anchoredPosition3D = new Vector3(x, rectTransform.anchoredPosition3D.y, rectTransform.anchoredPosition3D.z);
        }

        public static void SetHeight(this RectTransform rectTransform, float h)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
        }

        //https://forum.unity.com/threads/change-the-value-of-a-toggle-without-triggering-onvaluechanged.275056/#post-2750271
        #if !UNITY_2019_1_OR_NEWER
        private static Toggle.ToggleEvent emptyToggleEvent = new Toggle.ToggleEvent();
        #endif

        // added to 2019.1 https://docs.unity3d.com/2019.1/Documentation/ScriptReference/UI.Toggle.SetIsOnWithoutNotify.html
        public static void SetValue(this Toggle toggle, bool isOn)
        {
            #if UNITY_2019_1_OR_NEWER
            toggle.SetIsOnWithoutNotify(isOn);
            #else
            Toggle.ToggleEvent originalEvent = toggle.onValueChanged;
            toggle.onValueChanged = emptyToggleEvent;
            toggle.isOn = isOn;
            toggle.onValueChanged = originalEvent;
            #endif
        }

        #if !UNITY_2019_1_OR_NEWER
        private static Slider.SliderEvent emptySliderEvent = new Slider.SliderEvent();
        #endif

        public static void SetValue(this Slider slider, float v)
        {
            #if UNITY_2019_1_OR_NEWER
            slider.SetValueWithoutNotify(v);
            #else
            Slider.SliderEvent originalEvent = slider.onValueChanged;
            slider.onValueChanged = emptySliderEvent;
            slider.value = v;
            slider.onValueChanged = originalEvent;
            #endif
        }

        #if !UNITY_2019_1_OR_NEWER
        private static InputField.OnChangeEvent emptyInputFieldEvent = new InputField.OnChangeEvent();
        private static InputField.SubmitEvent emptyInputFieldSubmitEvent = new InputField.SubmitEvent();
        #endif

        public static void SetValue(this InputField inputField, string v)
        {
            #if UNITY_2019_1_OR_NEWER
            inputField.SetTextWithoutNotify(v);
            #else
            InputField.OnChangeEvent origianlEvent = inputField.onValueChanged;
            InputField.SubmitEvent originalSubmitEvent = inputField.onEndEdit;
            inputField.onValueChanged = emptyInputFieldEvent;
            inputField.onEndEdit = emptyInputFieldSubmitEvent;
            inputField.text = v;
            inputField.onValueChanged = origianlEvent;
            inputField.onEndEdit = originalSubmitEvent;
            #endif
        }

        // https://forum.unity.com/threads/deleting-all-chidlren-of-an-object.92827/#post-2058407
        /// <summary>
        /// Calls GameObject.Destroy on all children of transform. and immediately detaches the children
        /// from transform so after this call transform.childCount is zero.
        /// </summary>
        public static void DestroyChildren(this Transform transform)
        {
            if (transform && transform != null)
            {
                for (int i = transform.childCount - 1; i >= 0; --i)
                {
                    Object.Destroy(transform.GetChild(i).gameObject);
                }
                transform.DetachChildren();
            }
        }

        public static void Hide(this CanvasGroup canvasGroup, bool blockRaycasts = false, bool interactable = false)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = blockRaycasts;
            canvasGroup.interactable = interactable;
        }

        public static void Show(this CanvasGroup canvasGroup, bool blockRaycasts = true, bool interactable = true)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = blockRaycasts;
            canvasGroup.interactable = interactable;
        }

        public static bool IsHidden(this CanvasGroup canvasGroup)
        {
            return canvasGroup.alpha <= 0f;
        }

        public static bool IsShown(this CanvasGroup canvasGroup)
        {
            return canvasGroup.alpha > 0f;
        }

        public static void SetSingleOnClickCallback(this Button button, UnityAction action)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        public static void SetSingleOnValueChangedCallback(this Toggle toggle, UnityAction<bool> action)
        {
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(action);
        }

        public static void SetSingleOnValueChangedCallback(this InputField inputField, UnityAction<string> action)
        {
            inputField.onValueChanged.RemoveAllListeners();
            inputField.onValueChanged.AddListener(action);
        }

        public static void SetSingleOnEndEditCallback(this InputField inputField, UnityAction<string> action)
        {
            inputField.onEndEdit.RemoveAllListeners();
            inputField.onEndEdit.AddListener(action);
        }

        public static void SetSingleOnValueChangedCallback(this Dropdown inputField, UnityAction<int> action)
        {
            inputField.onValueChanged.RemoveAllListeners();
            inputField.onValueChanged.AddListener(action);
        }

        public static void SetSingleOnValueChangedCallback(this Slider slider, UnityAction<float> action)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(action);
        }
    }
}