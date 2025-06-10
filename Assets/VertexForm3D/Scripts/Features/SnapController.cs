#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SnapController : MonoBehaviour
{
    public GameObject objectToSnap;
    public Transform target;
    [SerializeField] GameObject visual;
    public snapType snapType;
    XRGrabInteractable grabInteractable;

    void Start()
    {

    }

    public void ShowVisual()
    {
        if (visual!=null)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Destroy(visual);
            }
            else
            {
                DestroyImmediate(visual);
            }
#else
            Destroy(visual);
#endif
        }
        visual = Instantiate(objectToSnap, target.position, target.rotation);
    }


    public void SetObjectToSnap(GameObject snapObject)
    {
        objectToSnap = snapObject;
    }
    public void SetTarget(Transform tr)
    {
        target = tr;
        Debug.Log("Target have been set: ", tr);
    }
    public void Snap()
    {
        Debug.Log("Snap called");
        if (objectToSnap.GetComponent<XRGrabInteractable>())
        {
            grabInteractable = objectToSnap.GetComponent<XRGrabInteractable>();
            Debug.Log("Snap grabInteractable: " + grabInteractable.name);
            DisableGrabbing();
        }
        else
        {
            Debug.Log("Snap grabInteractable: is null");
        }
            switch (snapType)
            {
                case snapType.position:
                    SnapPosition();
                    break;
                case snapType.rotation:
                    SnapRotation();
                    break;
                case snapType.scale:
                    SnapScale();
                    break;
                case snapType.positionAndRotation:
                    SnapPosition();
                    SnapRotation();
                    break;
                case snapType.positionAndScale:
                    SnapPosition();
                    SnapScale();
                    break;
                case snapType.rotationAndScale:
                    SnapRotation();
                    SnapScale();
                    break;
                case snapType.all:
                    SnapPosition();
                    SnapRotation();
                    SnapScale();
                    break;
                default:
                    break;
            }
    }

    public void SnapPosition()
    {
        objectToSnap.transform.position = target.position;
        Debug.Log("Snap position");
    }

    void EnableGrabbing()
    {
        grabInteractable.enabled = true;
        //grabInteractable = null;
    }

    void DisableGrabbing()
    {
        grabInteractable.enabled = false;
        Invoke(nameof(EnableGrabbing), .5f);
    }
    public void SnapRotation()
    {
        objectToSnap.transform.rotation = target.rotation;
        Debug.Log("Snap Rotation");
    }

    public void SnapScale()
    {
        objectToSnap.transform.localScale = target.localScale;
        Debug.Log("Snap scale");
    }
}

public enum snapType
{
    position,
    rotation,
    scale,
    positionAndRotation,
    positionAndScale,
    rotationAndScale,
    all
}

#if UNITY_EDITOR

[CustomEditor(typeof(SnapController))]
public class SnapControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Get the target script
        SnapController snapController = (SnapController)target;

        // Add a button to trigger ShowVisual
        if (GUILayout.Button("Show Visual"))
        {
            snapController.ShowVisual();
        }
    }
}

#endif