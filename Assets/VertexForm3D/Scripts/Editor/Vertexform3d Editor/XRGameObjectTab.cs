using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using UnityEditor.SceneManagement;
using Fusion;
using Fusion.Addons.Physics;

namespace VertexFormCore.Editor
{
    public class XRGameObjectTab : ITabGroup
    {
        private List<ToolkitItem> items = new List<ToolkitItem>();
        public string Name => "XR GAME OBJECTS";

        public string Description => "Tools for object interactivity, player movement, scene transitions, and teleportation.";
        public bool HasSubTabs => false;

        public List<SubTabCategory> GetSubTabCategories()
        {
            return new List<SubTabCategory>();
        }

        public List<ToolkitItem> GetItems()
        {
            return items;
        }
        public void InitializeItems()
        {
            items.Clear();

            // Sample Objects Category Items
            // items.Add(new ToolkitItem(
            //     "Create Cube Grabbable",
            //     "Adds a sample Cube to the Scene, with grabbing enabled, without automatic respawning.",
            //     "",
            //     CreateGrabNetworkedObject));

            // items.Add(new ToolkitItem(
            //     "Create Cube Grabbable With Respawn",
            //     "Adds a sample Cube to the Scene, with grabbing and automatic respawning at the Cube's original location enabled.",
            //     "",
            //     CreateRespawnableGrabNetworkedObject));

            items.Add(new ToolkitItem(
                "Create SnapAndSwap",
                "Adds a pair of grabbable sample Game Objects to the Scene, with snapping and swapping enabled between the two.",
                "",
                CreateSnapAndSwap));

            // Grabbing Category Items
            items.Add(new ToolkitItem(
                "Attach Trigger Event",
                "Adds Trigger Event to selected Game Object, to do actions onTriggerEnter and OnTriggerExit.",
                "",
                AttachTriggerEvent));

            items.Add(new ToolkitItem(
                "Make Resizable",
                "Enables scaling of selected Game Object.",
                "",
                AttachScaling));

            // In-Scene Movement Category Items
            items.Add(new ToolkitItem(
                "Make Teleportation Area",
                "Makes the selected Game Object a Teleportation zone where players may teleport.",
                "",
                AttachTeleportationAreaNetworked));

            items.Add(new ToolkitItem(
                "Make Grabbable",
                "Enables grabbing of the selected Game Object without automatic respawning.",
                "",
                AttachGrabNetworkedNotRespawnableObject));

            items.Add(new ToolkitItem(
                "Make Grabbable With Respawn",
                "Enables grabbing of the selected Game Object with automatic respawning at grabbed location enabled.",
                "",
                AttachGrabNetworkedRespawnableObject));

            // Physics Category Items
            items.Add(new ToolkitItem(
                "Enable Object Gravity",
                "Enable gravity on the selected objects.",
                "",
                () => HandleGravity(true)));

            items.Add(new ToolkitItem(
                "Disable Object Gravity",
                "Disable gravity on the selected objects.",
                "",
                () => HandleGravity(false)));
        }

        #region OBJECT INTERACTION METHODS

        public void AttachTriggerEvent()
        {
            GameObject[] selectedObject = Selection.gameObjects;
            foreach (GameObject obj in selectedObject)
            {
                if (obj.GetComponent<TriggerEvent>() == null)
                {
                    obj.AddComponent<TriggerEvent>();
                    Debug.Log("TriggerEvent attached to " + obj.name);
                }
                else
                {
                    Debug.LogWarning("Already attached.");
                }
            }
        }


        public void AttachScaling()
        {
            GameObject[] selectedObject = Selection.gameObjects;
            foreach (GameObject obj in selectedObject)
            {
                AttachGrabNetworkedObject(obj);
                obj.GetComponent<XRGrabInteractable>().selectMode = InteractableSelectMode.Multiple;
                obj.GetComponent<XRGeneralGrabTransformer>().allowTwoHandedScaling = true;
                EditorSceneManager.MarkSceneDirty(obj.scene);
            }
        }

        private void AttachGrabNetworkedRespawnableObject()
        {
            GameObject[] selectedObject = Selection.gameObjects;
            foreach (GameObject obj in selectedObject)
            {
                AttachGrabNetworkedObject(obj);
                XRGrabNetworkInteractable grabNetworkInteractable = obj.GetComponent<XRGrabNetworkInteractable>();
                if (grabNetworkInteractable == null)
                {
                    Debug.LogError("Failed to configure XRGrabNetworkInteractable on " + obj.name);
                    continue;
                }

                grabNetworkInteractable.shouldReset = true;
                grabNetworkInteractable.SetInitialPosition();
                grabNetworkInteractable.SetInitialRotation();
                EditorSceneManager.MarkSceneDirty(obj.scene);
            }
        }

        private void AttachGrabNetworkedNotRespawnableObject()
        {
            GameObject[] selectedObject = Selection.gameObjects;
            foreach (GameObject obj in selectedObject)
            {
                AttachGrabNetworkedObject(obj);
                XRGrabNetworkInteractable grabNetworkInteractable = obj.GetComponent<XRGrabNetworkInteractable>();
                if (grabNetworkInteractable == null)
                {
                    Debug.LogError("Failed to configure XRGrabNetworkInteractable on " + obj.name);
                    continue;
                }

                grabNetworkInteractable.shouldReset = false;
                EditorSceneManager.MarkSceneDirty(obj.scene);
            }
        }

        private void AttachTeleportationAreaNetworked()
        {
            GameObject[] selectedObject = Selection.gameObjects;
            if (selectedObject.Length > 0)
            {
                foreach (GameObject obj in selectedObject)
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
                    EditorSceneManager.MarkSceneDirty(obj.scene);
                }
            }
            else
            {
                Debug.LogWarning("No object selected.");
            }
        }

        private void CreateGrabNetworkedObject()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/GrabNetworkedObject"));
            g.name = "GrabNetworkedObject";
            EditorGUIUtility.PingObject(g);
        }

        private void CreateRespawnableGrabNetworkedObject()
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            AttachGrabNetworkedObject(g);
            XRGrabNetworkInteractable grabNetworkInteractable = g.GetComponent<XRGrabNetworkInteractable>();
            if (grabNetworkInteractable == null)
            {
                Debug.LogError("Failed to configure XRGrabNetworkInteractable on " + g.name);
                return;
            }

            grabNetworkInteractable.shouldReset = true;
            grabNetworkInteractable.SetInitialPosition();
            grabNetworkInteractable.SetInitialRotation();
            EditorGUIUtility.PingObject(g);
        }

        public void CreateSnapAndSwap()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/SnapAndSwap"));
            g.name = "SnapAndSwap";
            EditorGUIUtility.PingObject(g);
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
                EditorSceneManager.MarkSceneDirty(obj.scene);
            }
        }

        private void AttachGrabNetworkedObject(GameObject obj)
        {
            AttachAppropriateCollider(obj);
            if (obj.GetComponent<Rigidbody>() == null) obj.AddComponent<Rigidbody>();
            if (obj.GetComponent<NetworkObject>() == null) obj.AddComponent<NetworkObject>();

            NetworkObject no = obj.GetComponent<NetworkObject>();
            no.IsSpawnable = true;

            // Configure NetworkObject flags - only enable AllowStateAuthorityOverride
            no.Flags = NetworkObjectFlags.V1 | NetworkObjectFlags.AllowStateAuthorityOverride;

            NetworkTransform networkTransform = obj.GetComponent<NetworkTransform>();
            if (networkTransform != null && obj.GetComponent<NetworkRigidbody3D>() == null)
            {
                Debug.LogWarning("Replacing NetworkTransform with NetworkRigidbody3D on " + obj.name + " because XRGrabNetworkInteractable requires Fusion physics sync.");
                Object.DestroyImmediate(networkTransform);
            }

            if (obj.GetComponent<NetworkRigidbody3D>() == null) obj.AddComponent<NetworkRigidbody3D>();
            if (obj.GetComponent<XRGeneralGrabTransformer>() == null) obj.AddComponent<XRGeneralGrabTransformer>();
            if (obj.GetComponent<XRGrabInteractable>() == null) obj.AddComponent<XRGrabInteractable>();

            XRGeneralGrabTransformer grabTransformer = obj.GetComponent<XRGeneralGrabTransformer>();
            XRGrabInteractable grabInteractable = obj.GetComponent<XRGrabInteractable>();

            if (grabInteractable == null || grabTransformer == null)
            {
                Debug.LogError("Failed to configure grab components on " + obj.name);
                return;
            }

            grabInteractable.AddMultipleGrabTransformer(grabTransformer);
            grabInteractable.AddSingleGrabTransformer(grabTransformer);
            grabInteractable.selectMode = InteractableSelectMode.Multiple;
            if (obj.GetComponent<XRGrabNetworkInteractable>() == null) obj.AddComponent<XRGrabNetworkInteractable>();
        }

        private bool HasColliderInHierarchy(GameObject obj)
        {
            // Check if the object itself has a collider
            if (obj.GetComponent<Collider>() != null)
                return true;

            // Check all children recursively
            foreach (Transform child in obj.GetComponentsInChildren<Transform>())
            {
                if (child.GetComponent<Collider>() != null)
                    return true;
            }
            return false;
        }

        private void AttachAppropriateCollider(GameObject obj)
        {
            // If no collider exists in hierarchy, add one to the root object
            if (!HasColliderInHierarchy(obj))
            {
                MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    // No mesh filter or mesh, add a default box collider
                    obj.AddComponent<BoxCollider>();
                    Debug.Log("Assigned default BoxCollider to " + obj.name);
                    return;
                }

                Mesh mesh = meshFilter.sharedMesh;
                string meshName = mesh.name.ToLower(); // Mesh name (not GameObject name)

                // Check for Unity's built-in primitive meshes
                if (meshName.Contains("cube"))
                {
                    obj.AddComponent<BoxCollider>();
                    Debug.Log("Assigned BoxCollider to " + obj.name);
                }
                else if (meshName.Contains("sphere"))
                {
                    obj.AddComponent<SphereCollider>();
                    Debug.Log("Assigned SphereCollider to " + obj.name);
                }
                else if (meshName.Contains("capsule"))
                {
                    obj.AddComponent<CapsuleCollider>();
                    Debug.Log("Assigned CapsuleCollider to " + obj.name);
                }
                else
                {
                    // For custom meshes, analyze geometric properties
                    AssignColliderBasedOnGeometry(obj, mesh);
                }
            }
        }

        private void AssignColliderBasedOnGeometry(GameObject obj, Mesh mesh)
        {
            // Get the bounding box of the mesh
            Bounds bounds = mesh.bounds;
            Vector3 size = bounds.size;

            // Calculate aspect ratios
            float xyRatio = size.x / size.y;
            float xzRatio = size.x / size.z;
            float yzRatio = size.y / size.z;

            // Heuristics to determine shape
            bool isBoxLike = Mathf.Abs(xyRatio - 1f) < 0.3f && Mathf.Abs(xzRatio - 1f) < 0.3f; // Roughly equal dimensions
            bool isSphereLike = Mathf.Abs(xyRatio - 1f) < 0.1f && Mathf.Abs(xzRatio - 1f) < 0.1f && Mathf.Abs(yzRatio - 1f) < 0.1f; // Very equal dimensions
            bool isCapsuleLike = (size.y > size.x * 1.5f && Mathf.Abs(xyRatio - xzRatio) < 0.2f) || (size.z > size.x * 1.5f && Mathf.Abs(xyRatio - yzRatio) < 0.2f); // One dimension is longer

            if (isSphereLike)
            {
                SphereCollider sphereCollider = obj.AddComponent<SphereCollider>();
                sphereCollider.radius = bounds.extents.magnitude; // Approximate radius
                Debug.Log("Assigned SphereCollider to " + obj.name + " (geometry-based)");
            }
            else if (isCapsuleLike)
            {
                CapsuleCollider capsuleCollider = obj.AddComponent<CapsuleCollider>();
                capsuleCollider.height = Mathf.Max(size.x, size.y, size.z); // Use longest dimension
                capsuleCollider.radius = Mathf.Max(size.x, size.z) / 2f; // Approximate radius
                Debug.Log("Assigned CapsuleCollider to " + obj.name + " (geometry-based)");
            }
            else if (isBoxLike)
            {
                BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
                boxCollider.size = size; // Use bounding box size
                Debug.Log("Assigned BoxCollider to " + obj.name + " (geometry-based)");
            }
            else
            {
                // Default to BoxCollider for unknown shapes
                BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
                boxCollider.size = size;
                Debug.Log("Assigned default BoxCollider to " + obj.name + " (unknown geometry)");
            }
        }

        #endregion
    }
}