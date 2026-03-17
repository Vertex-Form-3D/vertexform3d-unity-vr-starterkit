using Fusion;
using Fusion.Addons.Physics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace VertexFormCore.Editor
{
    [CustomEditor(typeof(XRGrabNetworkInteractable))]
    [CanEditMultipleObjects]
    public class XRGrabNetworkInteractableEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Removes the grabbable/networking components added by the Vertex Form toolkit and leaves the base GameObject in the scene.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(targets == null || targets.Length == 0))
            {
                if (GUILayout.Button("Remove Grabbable Setup", GUILayout.Height(28)))
                {
                    RemoveGrabbableSetupFromTargets();
                }
            }
        }

        private void RemoveGrabbableSetupFromTargets()
        {
            int removedCount = 0;

            foreach (Object selectedTarget in targets)
            {
                XRGrabNetworkInteractable interactable = selectedTarget as XRGrabNetworkInteractable;
                if (interactable == null || interactable.gameObject == null)
                    continue;

                RemoveGrabbableSetup(interactable.gameObject);
                removedCount++;
            }

            if (removedCount > 0)
            {
                Debug.Log("Removed grabbable setup from " + removedCount + " object(s).");
            }
        }

        private static void RemoveGrabbableSetup(GameObject obj)
        {
            Undo.RegisterFullObjectHierarchyUndo(obj, "Remove Grabbable Setup");

            RemoveComponent<XRGrabNetworkInteractable>(obj);
            RemoveComponent<XRGrabInteractable>(obj);
            RemoveComponent<XRGeneralGrabTransformer>(obj);
            RemoveComponent<NetworkRigidbody3D>(obj);
            RemoveComponent<NetworkTransform>(obj);
            RemoveComponent<NetworkObject>(obj);

            EditorSceneManager.MarkSceneDirty(obj.scene);
        }

        private static void RemoveComponent<T>(GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();
            if (component != null)
            {
                Undo.DestroyObjectImmediate(component);
            }
        }
    }
}
