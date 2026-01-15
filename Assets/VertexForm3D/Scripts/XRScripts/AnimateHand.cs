using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VertexFormCore
{
    public class AnimateHand : MonoBehaviour
    {
        public InputActionProperty pinchAnimationAction;
        public InputActionProperty gripAnimationAction;
        public Animator handAnimator;
        public NetworkObject networkObject;

        private void Start()
        {
            if (networkObject != null && !networkObject.HasStateAuthority)
            {
                Destroy(this);
            }
        }

        void Update()
        {
            float triggerValue = pinchAnimationAction.action.ReadValue<float>();
            handAnimator.SetFloat("Trigger", triggerValue);

            float gripValue = gripAnimationAction.action.ReadValue<float>();
            handAnimator.SetFloat("Grip", gripValue);
        }
    }
}