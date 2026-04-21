using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VertexFormCore
{

    public class InputData : MonoBehaviour
    {
        public InputDevice _rightController;
        public InputDevice _leftController;
        public InputDevice _HMD;

        public static InputData Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            if (!_rightController.isValid || !_leftController.isValid || !_HMD.isValid)
                InitializeInputDevices();
        }

        private void OnEnable()
        {
            InputDevices.deviceConnected += OnInputDevicesChanged;
            InputDevices.deviceDisconnected += OnInputDevicesChanged;
        }

        private void OnDisable()
        {
            InputDevices.deviceConnected -= OnInputDevicesChanged;
            InputDevices.deviceDisconnected -= OnInputDevicesChanged;
        }

        private void OnInputDevicesChanged(InputDevice device) =>
            InitializeInputDevices();

        private void InitializeInputDevices()
        {
            if (!_rightController.isValid)
                InitializeInputDevice(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, ref _rightController);
            if (!_leftController.isValid)
                InitializeInputDevice(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, ref _leftController);
            if (!_HMD.isValid)
                InitializeInputDevice(InputDeviceCharacteristics.HeadMounted, ref _HMD);
        }

        private static void InitializeInputDevice(InputDeviceCharacteristics inputCharacteristics, ref InputDevice inputDevice)
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(inputCharacteristics, devices);

            if (devices.Count > 0)
                inputDevice = devices[0];
        }
    }
}