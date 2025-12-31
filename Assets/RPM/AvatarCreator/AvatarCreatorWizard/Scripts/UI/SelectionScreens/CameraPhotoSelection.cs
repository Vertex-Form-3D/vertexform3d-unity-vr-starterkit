using System;
using System.Linq;
using ReadyPlayerMe.AvatarCreator;
using UnityEngine;
using UnityEngine.UI;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public class CameraPhotoSelection : State
    {
        [SerializeField] private RawImage rawImage;
        [SerializeField] private Button cameraButton;

        public override StateType StateType => StateType.CameraPhoto;
        public override StateType NextState => StateType.Editor;
#if !RPM_DISABLE_CAMERA_PERMISSION
      private WebCamTexture camTexture;
#endif
        public override async void ActivateState()
        {
            cameraButton.onClick.AddListener(OnCameraButton);
            if (!AuthManager.IsSignedIn && !AuthManager.IsSignedInAnonymously)
            {
                await AuthManager.LoginAsAnonymous();
            }
            OpenCamera();
        }

        public override void DeactivateState()
        {
            cameraButton.onClick.RemoveListener(OnCameraButton);
            CloseCamera();
        }

        private void OpenCamera()
        {
#if !RPM_DISABLE_CAMERA_PERMISSION
            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                return;
            }

            rawImage.color = Color.white;

            var webCamDevice = devices.FirstOrDefault(device => device.isFrontFacing);
            if (webCamDevice.Equals(default(WebCamDevice)))
            {
                webCamDevice = devices[0];
            }

            var size = rawImage.rectTransform.sizeDelta;
            camTexture = new WebCamTexture(webCamDevice.name, (int) size.x, (int) size.y);
            camTexture.Play();
            rawImage.texture = camTexture;
            rawImage.SizeToParent();
#endif
        }

        private void CloseCamera()
        {
#if !RPM_DISABLE_CAMERA_PERMISSION
            if (camTexture != null && camTexture.isPlaying)
            {
                camTexture.Stop();
            }
#endif
        }

        private void OnCameraButton()
        {
#if !RPM_DISABLE_CAMERA_PERMISSION
            if (camTexture == null || !camTexture.isPlaying)
            {
                LoadingManager.EnableLoading("Camera is not available.", LoadingManager.LoadingType.Popup, false);
                return;
            }

            var texture = new Texture2D(rawImage.texture.width, rawImage.texture.height, TextureFormat.ARGB32, false);
            texture.SetPixels(camTexture.GetPixels());
            texture.Apply();

            var bytes = texture.EncodeToPNG();

            AvatarCreatorData.AvatarProperties.Id = string.Empty;
            AvatarCreatorData.AvatarProperties.Base64Image = Convert.ToBase64String(bytes);
            AvatarCreatorData.AvatarProperties.isDraft = true;
            AvatarCreatorData.IsExistingAvatar = false;

            StateMachine.SetState(StateType.Editor);
#endif
        }
    }
}
