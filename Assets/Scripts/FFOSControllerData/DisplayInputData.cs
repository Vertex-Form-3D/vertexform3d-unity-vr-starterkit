using UnityEngine;
using UnityEngine.XR;
using TMPro;

namespace VertextFormCore
{

    [RequireComponent(typeof(InputData))]
    public class DisplayInputData : MonoBehaviour
    {
        public TextMeshProUGUI leftScoreDisplay;
        public TextMeshProUGUI rightScoreDisplay;
        public HandControllerClass leftControllerDebug = new HandControllerClass();
        public HandControllerClass rightControllerDebug = new HandControllerClass();
        private InputData _inputData;
        private float _leftMaxScore = 0f;
        private float _rightMaxScore = 0f;

        private void Start()
        {
            _inputData = GetComponent<InputData>();
        }
        // Update is called once per frame
        void Update()
        {
            /*if (_inputData._leftController.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 leftVelocity))
            {
                _leftMaxScore = Mathf.Max(leftVelocity.magnitude, _leftMaxScore);
                leftScoreDisplay.text = _leftMaxScore.ToString("F2");
            }*/

            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.grip, out float leftGrip))
            {
                leftControllerDebug.gripText.text = "Grip: " + leftGrip.ToString();
            }

            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.gripButton, out bool leftGripBtn))
            {
                leftControllerDebug.gripBtnText.text = "GripButton: " + leftGripBtn.ToString();
            }

            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.trigger, out float leftTrigger))
            {
                leftControllerDebug.triggerText.text = "trigger: " + leftTrigger.ToString();
            }
            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftTriggerBtn))
            {
                leftControllerDebug.triggerBtnText.text = "triggerButton: " + leftTriggerBtn.ToString();
            }

            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftprimaryButton))
            {
                leftControllerDebug.primaryButtonText.text = "primaryButton: " + leftprimaryButton.ToString();
            }

            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.primaryTouch, out bool leftprimaryTouch))
            {
                leftControllerDebug.primaryTouchText.text = "primaryTouch: " + leftprimaryTouch.ToString();
            }

            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftprimary2DAxis))
            {
                leftControllerDebug.primary2DAxisText.text = "primary2DAxis: " + leftprimary2DAxis.ToString();
            }

            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool leftprimary2DAxisClick))
            {
                leftControllerDebug.primary2DAxisClickText.text = "primary2DAxisClick: " + leftprimary2DAxisClick.ToString();
            }

            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out bool leftprimary2DAxisTouch))
            {
                leftControllerDebug.primary2DAxisTouchText.text = "primary2DAxisTouch: " + leftprimary2DAxisTouch.ToString();
            }

            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool leftsecondaryButton))
            {
                leftControllerDebug.secondaryButtonText.text = "secondaryButton: " + leftsecondaryButton.ToString();
            }

            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.secondaryTouch, out bool leftsecondaryTouch))
            {
                leftControllerDebug.secondaryTouchText.text = "secondaryTouch: " + leftsecondaryTouch.ToString();
            }

            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.secondary2DAxis, out Vector2 leftsecondary2DAxis))
            {
                leftControllerDebug.secondary2DAxisText.text = "secondary2DAxis: " + leftsecondary2DAxis.ToString();
            }

            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.secondary2DAxisClick, out bool leftsecondary2DAxisClick))
            {
                leftControllerDebug.secondary2DAxisClickText.text = "secondary2DAxisClick: " + leftsecondary2DAxisClick.ToString();
            }

            if (_inputData._leftController.TryGetFeatureValue(CommonUsages.secondary2DAxisTouch, out bool leftsecondary2DAxisTouch))
            {
                leftControllerDebug.secondary2DAxisTouchText.text = "secondary2DAxisTouch: " + leftsecondary2DAxisTouch.ToString();
            }





            /*if (_inputData._rightController.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 rightVelocity))
            {
                _rightMaxScore = Mathf.Max(rightVelocity.magnitude, _rightMaxScore);
                rightScoreDisplay.text = _rightMaxScore.ToString("F2");
            }*/

            if (_inputData._rightController.TryGetFeatureValue(CommonUsages.grip, out float rightGrip))
            {
                rightControllerDebug.gripText.text = "grip: " + rightGrip.ToString();
            }

            if (_inputData._rightController.TryGetFeatureValue(CommonUsages.gripButton, out bool rightgripButton))
            {
                rightControllerDebug.gripBtnText.text = "gripButton: " + rightgripButton.ToString();
            }

            if (_inputData._rightController.TryGetFeatureValue(CommonUsages.trigger, out float rightTrigger))
            {
                rightControllerDebug.triggerText.text = "trigger: " + rightTrigger.ToString();
            }
            if (_inputData._rightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightTriggerBtn))
            {
                rightControllerDebug.triggerBtnText.text = "triggerButton: " + rightTriggerBtn.ToString();
            }

            if (_inputData._rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool rightprimaryButton))
            {
                rightControllerDebug.primaryButtonText.text = "primaryButton: " + rightprimaryButton.ToString();
            }

            if (_inputData._rightController.TryGetFeatureValue(CommonUsages.primaryTouch, out bool rightprimaryTouch))
            {
                rightControllerDebug.primaryTouchText.text = "primaryTouch: " + rightprimaryTouch.ToString();
            }

            if (_inputData._rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightprimary2DAxis))
            {
                rightControllerDebug.primary2DAxisText.text = "primary2DAxis: " + rightprimary2DAxis.ToString();
            }

            if (_inputData._rightController.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool rightprimary2DAxisClick))
            {
                rightControllerDebug.primary2DAxisClickText.text = "primary2DAxisClick: " + rightprimary2DAxisClick.ToString();
            }

            if (_inputData._rightController.TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out bool rightprimary2DAxisTouch))
            {
                rightControllerDebug.primary2DAxisTouchText.text = "primary2DAxisTouch: " + rightprimary2DAxisTouch.ToString();
            }

            if (_inputData._rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool rightsecondaryButton))
            {
                rightControllerDebug.secondaryButtonText.text = "secondaryButton: " + rightsecondaryButton.ToString();
            }

            if (_inputData._rightController.TryGetFeatureValue(CommonUsages.secondaryTouch, out bool rightsecondaryTouch))
            {
                rightControllerDebug.secondaryTouchText.text = "secondaryTouch: " + rightsecondaryTouch.ToString();
            }

            if (_inputData._rightController.TryGetFeatureValue(CommonUsages.secondary2DAxis, out Vector2 rightsecondary2DAxis))
            {
                rightControllerDebug.secondary2DAxisText.text = "secondary2DAxis: " + rightsecondary2DAxis.ToString();
            }

            if (_inputData._rightController.TryGetFeatureValue(CommonUsages.secondary2DAxisClick, out bool rightsecondary2DAxisClick))
            {
                rightControllerDebug.secondary2DAxisClickText.text = "secondary2DAxisClick: " + rightsecondary2DAxisClick.ToString();
            }

            if (_inputData._rightController.TryGetFeatureValue(CommonUsages.secondary2DAxisTouch, out bool rightsecondary2DAxisTouch))
            {
                rightControllerDebug.secondary2DAxisTouchText.text = "secondary2DAxisTouch: " + rightsecondary2DAxisTouch.ToString();
            }
        }
    }

    [System.Serializable]
    public class HandControllerClass
    {
        public TextMeshProUGUI gripText;
        public TextMeshProUGUI gripBtnText;
        public TextMeshProUGUI triggerText;
        public TextMeshProUGUI triggerBtnText;
        public TextMeshProUGUI joystickText;
        public TextMeshProUGUI primaryButtonText;
        public TextMeshProUGUI primaryTouchText;
        public TextMeshProUGUI primaryJoystickText;
        public TextMeshProUGUI primary2DAxisText;
        public TextMeshProUGUI primary2DAxisClickText;
        public TextMeshProUGUI primary2DAxisTouchText;

        public TextMeshProUGUI secondaryButtonText;
        public TextMeshProUGUI secondaryTouchText;
        public TextMeshProUGUI secondaryJoystickText;
        public TextMeshProUGUI secondary2DAxisText;
        public TextMeshProUGUI secondary2DAxisClickText;
        public TextMeshProUGUI secondary2DAxisTouchText;
    }
}