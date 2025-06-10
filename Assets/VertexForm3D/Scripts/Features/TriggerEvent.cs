#if UNITY_EDITOR
using UnityEditor;
# endif
using UnityEngine;
using UnityEngine.Events;

[System.Serializable] public class EnterEvent : UnityEvent<GameObject> { }
[System.Serializable] public class ExitEvent : UnityEvent<GameObject> { }
public class TriggerEvent : MonoBehaviour
{
    public TriggerType triggerType;
    public string Tag;
    public GameObject gameObjectToCheck;
    public bool callEnterOnce = true;
    public bool callExitOnce = true;
    public EnterEvent ontriggerEnterEvent = new EnterEvent();
    public ExitEvent ontriggerExitEvent = new ExitEvent();
    bool EnterEventCalled;
    bool ExitEventCalled;


    private void OnTriggerEnter(Collider other)
    {
        switch (triggerType)
        {
            case TriggerType.Tag:
                if (other.tag == Tag)
                {
                    if (callEnterOnce)
                    {
                        if (!EnterEventCalled)
                        {
                            if (ontriggerEnterEvent != null)
                            {
                                ontriggerEnterEvent?.Invoke(other.gameObject);
                                Debug.Log("Trigger Enter Event Called");
                            }
                            EnterEventCalled = true;
                        }
                    }
                    else
                    {
                        if (ontriggerEnterEvent != null)
                        {
                            ontriggerEnterEvent?.Invoke(other.gameObject);
                            Debug.Log("Trigger Enter Event Called2");
                        }
                    }
                }
                break;
            case TriggerType.gameObject:
                if (other.gameObject == gameObjectToCheck)
                {
                    if (callEnterOnce)
                    {
                        if (!EnterEventCalled)
                        {
                            if (ontriggerEnterEvent != null)
                            {
                                ontriggerEnterEvent?.Invoke(other.gameObject);
                                Debug.Log("Trigger Enter Event Called");
                            }
                            EnterEventCalled = true;
                        }
                    }
                    else
                    {
                        if (ontriggerEnterEvent != null)
                        {
                            ontriggerEnterEvent?.Invoke(other.gameObject);
                            Debug.Log("Trigger Enter Event Called2");
                        }
                    }
                }
                break;
            default:
                break;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        switch (triggerType)
        {
            case TriggerType.Tag:
                if (other.tag == Tag)
                {
                    if (callExitOnce)
                    {
                        if (!ExitEventCalled)
                        {
                            if (ontriggerExitEvent != null)
                            {
                                ontriggerExitEvent?.Invoke(other.gameObject);
                            }
                            ExitEventCalled = true;
                        }
                    }
                    else
                    {
                        if (ontriggerExitEvent != null)
                        {
                            ontriggerExitEvent?.Invoke(other.gameObject);
                        }
                    }
                }
                break;
            case TriggerType.gameObject:
                if (other.gameObject == gameObjectToCheck)
                {
                    if (callExitOnce)
                    {
                        if (!ExitEventCalled)
                        {
                            if (ontriggerExitEvent != null)
                            {
                                ontriggerExitEvent?.Invoke(other.gameObject);
                            }
                            ExitEventCalled = true;
                        }
                    }
                    else
                    {
                        if (ontriggerExitEvent != null)
                        {
                            ontriggerExitEvent?.Invoke(other.gameObject);
                        }
                    }
                }
                break;
            default:
                break;
        }        
    }
}

public enum TriggerType
{
    Tag,
    gameObject
}

#if UNITY_EDITOR

[CustomEditor(typeof(TriggerEvent))]
public class TriggerEventEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Get the target script
        TriggerEvent triggerEvent = (TriggerEvent)target;

        // Draw the triggerType enum field
        EditorGUILayout.PropertyField(serializedObject.FindProperty("triggerType"));

        // Show fields based on triggerType
        if (triggerEvent.triggerType == TriggerType.Tag)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Tag"));
        }
        else if (triggerEvent.triggerType == TriggerType.gameObject)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gameObjectToCheck"));
        }

        // Draw the rest of the fields
        EditorGUILayout.PropertyField(serializedObject.FindProperty("callEnterOnce"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("callExitOnce"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ontriggerEnterEvent"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ontriggerExitEvent"));

        // Apply changes to serialized properties
        serializedObject.ApplyModifiedProperties();
    }
}
#endif