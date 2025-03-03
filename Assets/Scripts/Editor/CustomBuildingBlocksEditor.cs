using UnityEngine;
using UnityEditor;
using VertextFormCore;
using Photon.Pun;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[CustomEditor(typeof(GameObject))]
public class CustomBuildingBlocksEditor : EditorWindow
{
    [MenuItem("VertexForm3D SDK/Custom Building Blocks")]
    public static void ShowWindow()
    {
        CustomBuildingBlocksEditor window = GetWindow<CustomBuildingBlocksEditor>("Custom Building Blocks");
        window.minSize = new Vector2(400, 300); // Ensure a reasonable size
        window.Show();
    }

    private Vector2 scrollPosition;
    private Texture2D banner;

    private void OnEnable()
    {
        banner = Resources.Load<Texture2D>("VF3DBannerEditor");
    }

    private void OnGUI()
    {
        GUILayout.Space(5); // Reduce top padding

        if (banner != null)
        {
            float bannerWidth = Mathf.Min(banner.width, position.width - 10); // Fit to window width
            float bannerHeight = (bannerWidth / banner.width) * banner.height; // Maintain aspect ratio
            GUILayout.Label(banner, GUILayout.Width(bannerWidth), GUILayout.Height(bannerHeight), GUILayout.ExpandWidth(true));
        }
        else
        {
            EditorGUILayout.HelpBox("Banner image not found. Make sure 'VF3DEditorBanner' is inside the Resources folder.", MessageType.Warning);
        }

        GUILayout.Space(10);

        // Draw a horizontal line to separate sections
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.ExpandWidth(true));

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUILayout.ExpandWidth(true));

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

        DrawSection("Make this a teleportation area", "Convert the selected object into a teleportation zone.", "Make this a teleportation area", AttachTeleportationAreaNetworked);
        DrawSection("Add Grab and No Respawn", "Allow grabbing without respawning.", "Apply Grab and No respawn", AttachGrabNetworkedNotRespawnableObject);
        DrawSection("Add Grab and Respawn", "Allow grabbing with automatic respawning.", "Apply Grab and respawn", AttachGrabNetworkedRespawnableObject);
        DrawSection("Create Grab and Respawn Cube", "Create a sample cube with grab and respawn enabled.", "Create Cube", CreateRespawnableGrabNetworkedObject);
        DrawSection("Create Grab and No Respawn Cube", "Create a sample cube with grab functionality without respawning.", "Create Cube", CreateGrabNetworkedObject);
        DrawSection("Networked Scene", "Apply networked settings to the entire scene.", "Make Scene Networked", NetworkedScene);
        DrawSection("Handle Object Gravity", "Enable or disable gravity on the selected objects.", "Enable Gravity", () => HandleGravity(true));
        DrawSection("Handle Object Gravity", "Enable or disable gravity on the selected objects.", "Disable Gravity", () => HandleGravity(false));

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.EndScrollView();
    }

    private void DrawSection(string title, string description, string buttonText, System.Action action)
    {
        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
        GUILayout.Label(title, EditorStyles.boldLabel);
        GUILayout.Label(description, EditorStyles.wordWrappedLabel);
        if (GUILayout.Button(buttonText, GUILayout.Height(30), GUILayout.ExpandWidth(true)))
        {
            action.Invoke();
        }
        GUILayout.ExpandWidth(true);
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);
    }

    private void AttachTeleportationAreaNetworked()
    {
        GameObject[] selectedObject = Selection.gameObjects;
        if (selectedObject.Length > 0)
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                if (obj.GetComponent<TeleportationAreaNetworked>() == null)
                {
                    obj.AddComponent<TeleportationAreaNetworked>();
                    Debug.Log("TeleportationAreaNetworked attached to " + obj.name);
                }
                else
                {
                    Debug.LogWarning("Already attached.");
                }
            }
        }
        else
        {
            Debug.LogWarning("No object selected.");
        }
    }

    private void AttachGrabNetworkedNotRespawnableObject()
    {
        GameObject[] selectedObject = Selection.gameObjects;
        if (selectedObject.Length > 0)
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                AttachGrabNetworkedObject(obj);
                obj.GetComponent<XRGrabNetworkInteractable>().shouldReset = false;
            }
        }
    }

    private void AttachGrabNetworkedRespawnableObject()
    {
        GameObject[] selectedObject = Selection.gameObjects;
        if (selectedObject.Length > 0)
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                AttachGrabNetworkedObject(obj);
                obj.GetComponent<XRGrabNetworkInteractable>().shouldReset = true;
                obj.GetComponent<XRGrabNetworkInteractable>().SetInitialPosition();
                obj.GetComponent<XRGrabNetworkInteractable>().SetInitialRotation();
            }
        }
    }

    private void NetworkedScene()
    {
        XRGrabNetworkInteractable[] objects = FindObjectsByType<XRGrabNetworkInteractable>(FindObjectsSortMode.InstanceID);
        foreach (var obj in objects)
        {
            if (obj.shouldReset)
            {
                obj.SetInitialPosition();
                obj.SetInitialRotation();
            }
        }
    }

    private void CreateGrabNetworkedObject()
    {
        GameObject g = Instantiate(Resources.Load<GameObject>("CustomEditor/GrabNetworkedObject"));
        g.name = "GrabNetworkedObject";
    }

    private void CreateRespawnableGrabNetworkedObject()
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        AttachGrabNetworkedObject(g);
        g.GetComponent<XRGrabNetworkInteractable>().shouldReset = true;
        g.GetComponent<XRGrabNetworkInteractable>().SetInitialPosition();
        g.GetComponent<XRGrabNetworkInteractable>().SetInitialRotation();
    }

    private void AttachGrabNetworkedObject(GameObject obj)
    {
        if (obj.GetComponent<Collider>() == null) obj.AddComponent<Collider>();
        if (obj.GetComponent<Rigidbody>() == null) obj.AddComponent<Rigidbody>();
        if (obj.GetComponent<PhotonView>() == null) obj.AddComponent<PhotonView>();

        PhotonView pv = obj.GetComponent<PhotonView>();
        pv.OwnershipTransfer = OwnershipOption.Takeover;
        if (obj.GetComponent<PhotonTransformView>() == null) obj.AddComponent<PhotonTransformView>();

        if (obj.GetComponent<XRGeneralGrabTransformer>() == null) obj.AddComponent<XRGeneralGrabTransformer>();
        if (obj.GetComponent<XRGrabInteractable>() == null) obj.AddComponent<XRGrabInteractable>();
        if (obj.GetComponent<XRGrabNetworkInteractable>() == null) obj.AddComponent<XRGrabNetworkInteractable>();
    }

    private void HandleGravity(bool gravity)
    {
        GameObject[] selectedObject = Selection.gameObjects;
        foreach (GameObject obj in selectedObject)
        {
            if (obj.GetComponent<Rigidbody>() == null)
            {
                obj.AddComponent<Rigidbody>();
                Debug.Log("Rigidbody attached to " + obj.name);
            }
            obj.GetComponent<Rigidbody>().useGravity = gravity;
        }
    }
}
