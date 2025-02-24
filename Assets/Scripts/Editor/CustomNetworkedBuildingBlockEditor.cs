using UnityEngine;
using UnityEditor;
using VertextFormCore;
using System.Linq;
using Photon.Pun;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[CustomEditor(typeof(GameObject))]
public class CustomNetworkedBuildingBlockEditor : EditorWindow
{
    [MenuItem("Tools/CustomNetworkedBuildingBlockEditor")]
    public static void ShowWindow()
    {
        GetWindow<CustomNetworkedBuildingBlockEditor>("CustomNetworked Building Block Window");
    }

    private void OnGUI()
    {
        GUILayout.Label("make this a teleportation area", EditorStyles.boldLabel);

        if (GUILayout.Button("make this a teleportation area"))
        {
            AttachTeleportationAreaNetworked();
        }

        GUILayout.Label("Add Grab and No Respawn functionality to an existing 3D object", EditorStyles.boldLabel);

        if (GUILayout.Button("Grab and No respawn"))
        {
            AttachGrabNetworkedNotRespawnableObject();
        }
        GUILayout.Label("Add Grab and Respawn functionality to existing 3D object", EditorStyles.boldLabel);

        if (GUILayout.Button("Grab and respawn"))
        {
            AttachGrabNetworkedRespawnableObject();
        }

        GUILayout.Label("Create example Cube with Grab and Respawn functionality", EditorStyles.boldLabel);

        if (GUILayout.Button("create Grab and respawn"))
        {
            CreateRespawnableGrabNetworkedObject();
        }

        GUILayout.Label("Create example Cube with Grab and No Respawn functionality", EditorStyles.boldLabel);

        if (GUILayout.Button("create Grab and not respawn"))
        {
            CreateGrabNetworkedObject();
        }

        GUILayout.Label("Networked Scene", EditorStyles.boldLabel);

        if (GUILayout.Button("Make Scene Networked"))
        {
            NetworkedScene();
        }
        GUILayout.Label("Handle Object Gravity", EditorStyles.boldLabel);

        if (GUILayout.Button("Enable Gravity"))
        {
            HandleGravity(true);
        }
        if (GUILayout.Button("Disable Gravity"))
        {
            HandleGravity(false);
        }
    }

    private void AttachTeleportationAreaNetworked()
    {
        GameObject[] selectedObject = Selection.gameObjects;

        foreach (GameObject obj in Selection.gameObjects)
        {
            if (obj != null)
            {
                if (obj.GetComponent<TeleportationAreaNetworked>() == null)
                {
                    obj.AddComponent<TeleportationAreaNetworked>();
                    Debug.Log("TeleportationAreaNetworked script attached to " + obj.name);
                }
                else
                {
                    Debug.LogWarning("TeleportationAreaNetworked is already attached.");
                }
            }
            else
            {
                Debug.LogWarning("No object selected. Please select a GameObject from the hierarchy.");
            }
        }
    }
    private void AttachGrabNetworkedNotRespawnableObject()
    {
        GameObject[] selectedObject = Selection.gameObjects;

        foreach (GameObject obj in Selection.gameObjects)
        {
            AttachGrabNetworkedObject(obj);
        }
    }
    private void AttachGrabNetworkedRespawnableObject()
    {
        GameObject[] selectedObject = Selection.gameObjects;

        foreach (GameObject obj in Selection.gameObjects)
        {
            AttachGrabNetworkedObject(obj);
            GameObject g = obj;
            g.GetComponent<XRGrabNetworkInteractable>().shouldReset = true;
            g.GetComponent<XRGrabNetworkInteractable>().SetInitialPosition();
            g.GetComponent<XRGrabNetworkInteractable>().SetInitialRotation();
        }
    }

    private void NetworkedScene()
    {
        XRGrabNetworkInteractable[] XGNIs = FindObjectsByType<XRGrabNetworkInteractable>(FindObjectsSortMode.InstanceID);
        foreach (XRGrabNetworkInteractable item in XGNIs)
        {
            if (item.shouldReset)
            {
                item.SetInitialPosition();
                item.SetInitialRotation();
            }
        }
    }
    public void CreateGrabNetworkedObject()
    {
        GameObject g = Instantiate(Resources.Load<GameObject>("CustomEditor/GrabNetworkedObject"));
        g.name = "GrabNetworkedObject";
    }

    public void CreateRespawnableGrabNetworkedObject()
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        AttachGrabNetworkedObject(g);
        g.GetComponent<XRGrabNetworkInteractable>().shouldReset = true;
        g.GetComponent<XRGrabNetworkInteractable>().SetInitialPosition();
        g.GetComponent<XRGrabNetworkInteractable>().SetInitialRotation();
    }
    private void AttachGrabNetworkedObject(GameObject obj)
    {
        GameObject selectedObject = obj;

        if (selectedObject != null)
        {
            if (selectedObject.GetComponent<Collider>() == null)
            {
                selectedObject.AddComponent<Collider>();
            }
            if (selectedObject.GetComponent<Rigidbody>() == null)
            {
                selectedObject.AddComponent<Rigidbody>();
            }
            if (selectedObject.GetComponent<PhotonView>() == null)
            {
                selectedObject.AddComponent<PhotonView>();
            }
            PhotonView pv = selectedObject.GetComponent<PhotonView>();
            pv.OwnershipTransfer = OwnershipOption.Takeover;
            if (selectedObject.GetComponent<PhotonTransformView>() == null)
            {
                selectedObject.AddComponent<PhotonTransformView>();
            }
            PhotonTransformView ptv = selectedObject.GetComponent<PhotonTransformView>();
            ptv.m_UseLocal = false;
            if (selectedObject.GetComponent<XRGeneralGrabTransformer>() == null)
            {
                selectedObject.AddComponent<XRGeneralGrabTransformer>();
            }
            if (selectedObject.GetComponent<XRGrabInteractable>() == null)
            {
                selectedObject.AddComponent<XRGrabInteractable>();
            }
            if (selectedObject.GetComponent<XRGrabNetworkInteractable>() == null)
            {
                selectedObject.AddComponent<XRGrabNetworkInteractable>();
                Debug.Log("XRGrabNetworkInteractable script attached to " + selectedObject.name);
            }
            else
            {
                Debug.LogWarning("XRGrabNetworkInteractable is already attached.");
            }
        }
        else
        {
            Debug.LogWarning("No object selected. Please select a GameObject from the hierarchy.");
        }
    }
    private void HandleGravity(bool gravity)
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject != null)
        {
            if (selectedObject.GetComponent<Rigidbody>() == null)
            {
                selectedObject.AddComponent<Rigidbody>();
                Debug.Log("Rigidbody attached to " + selectedObject.name);
            }
            selectedObject.GetComponent<Rigidbody>().useGravity = gravity;
        }
        else
        {
            Debug.LogWarning("No object selected. Please select a GameObject from the hierarchy.");
        }
    }
}
