using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Fusion;

[System.Serializable]
public class AnimationInput
{
    public string animationPropertyName;
    public InputActionProperty action;
}

public class AnimateOnInput : MonoBehaviour
{
    public List<AnimationInput> animationInputs;
    public Animator animator;
    public NetworkObject networkObject;
    void Update()
    {
        if (networkObject != null)
        {
            if (!networkObject.HasInputAuthority)
            {
                return;
            }
        }
        foreach (var item in animationInputs)
        {
            float actionValue = 0f; // Default to zero

            // Only use the input value if the action is being performed
            if (item.action.action.IsPressed() || item.action.action.triggered)
            {
                actionValue = item.action.action.ReadValue<float>();
            }

            animator.SetFloat(item.animationPropertyName, actionValue);
        }
    }
}