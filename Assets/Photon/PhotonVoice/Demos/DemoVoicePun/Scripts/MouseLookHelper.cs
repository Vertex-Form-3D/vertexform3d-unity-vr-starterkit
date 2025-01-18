using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

[Serializable]
public class MouseLookHelper {
    public float XSensitivity = 2f;
    public float YSensitivity = 2f;
    public bool clampVerticalRotation = true;
    public float MinimumX = -90F;
    public float MaximumX = 90F;
    public bool smooth;
    public float smoothTime = 5f;

    private Quaternion m_CharacterTargetRot;
    private Quaternion m_CameraTargetRot;

    public void Init(Transform character, Transform camera) {
        this.m_CharacterTargetRot = character.localRotation;
        this.m_CameraTargetRot = camera.localRotation;
    }

    public void LookRotation(Transform character, Transform camera) {
        float yRot = CrossPlatformInputManager.GetAxis("Mouse X") * this.XSensitivity;
        float xRot = CrossPlatformInputManager.GetAxis("Mouse Y") * this.YSensitivity;

        this.m_CharacterTargetRot *= Quaternion.Euler(0f, yRot, 0f);
        this.m_CameraTargetRot *= Quaternion.Euler(-xRot, 0f, 0f);

        if (this.clampVerticalRotation) {
            this.m_CameraTargetRot = this.ClampRotationAroundXAxis(this.m_CameraTargetRot);
        }
        if (this.smooth) {
            character.localRotation = Quaternion.Slerp(character.localRotation, this.m_CharacterTargetRot,
                this.smoothTime * Time.deltaTime);
            camera.localRotation = Quaternion.Slerp(camera.localRotation, this.m_CameraTargetRot,
                this.smoothTime * Time.deltaTime);
        }
        else {
            character.localRotation = this.m_CharacterTargetRot;
            camera.localRotation = this.m_CameraTargetRot;
        }
    }

    private Quaternion ClampRotationAroundXAxis(Quaternion q) {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1.0f;

        float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);

        angleX = Mathf.Clamp(angleX, this.MinimumX, this.MaximumX);

        q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

        return q;
    }
}
