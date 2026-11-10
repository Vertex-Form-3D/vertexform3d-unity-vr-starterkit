using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VertexFormCore.Editor
{
    /// <summary>
    /// Puts a Sitting Prefab (SitSpot) on every chair of the open scene(s), copying how you placed the first one:
    /// where it sits on the chair, which way the SitPoint (the arrow in the Scene view) faces, and where its Sit button floats.
    ///
    ///   1. Put ONE Sitting Prefab on one chair and set its SitPoint and facing the way you like it (PLAYER POSITION > SittingPoint).
    ///   2. Menu: Vertex Form > Seating > Add Sit Spots To All Chairs   (or the same button in the Vertex Form window).
    ///
    /// Which objects count as chairs: whatever you have selected in the Hierarchy; if nothing is selected, every object whose name
    /// contains "chair" (and that has a mesh). Chairs that already have a sit spot on them are left alone.
    /// Chairs that are modelled facing another way can be fixed afterwards by turning their SitPoint.
    /// </summary>
    public static class SeatingTools
    {
        const float AlreadySeatedRadius = 0.6f;

        [MenuItem("Vertex Form/Seating/Add Sit Spots To All Chairs")]
        public static void AddToAllChairs()
        {
            var allSpots = new List<SitSpot>();
            foreach (var root in OpenRoots())
                allSpots.AddRange(root.GetComponentsInChildren<SitSpot>(true));

            // the one you placed by hand: the selected one, else the first in the scene
            SitSpot reference = null;
            if (Selection.activeGameObject != null) reference = Selection.activeGameObject.GetComponentInParent<SitSpot>();
            if (reference == null && allSpots.Count > 0) reference = allSpots[0];

            // candidate chairs
            var chairs = new List<Transform>();
            bool fromSelection = false;
            foreach (var go in Selection.gameObjects)
            {
                if (go == null || !go.scene.IsValid() || EditorUtility.IsPersistent(go)) continue;
                if (go.GetComponentInParent<SitSpot>() != null) continue;     // the sit spot itself is not a chair
                chairs.Add(go.transform);
            }
            fromSelection = chairs.Count > 0;
            if (!fromSelection)
                foreach (var t in AllTransforms())
                    if (LooksLikeChair(t) && HasMesh(t)) chairs.Add(t);

            chairs = TopMost(chairs);

            // the chair the reference sits on (to copy its offset)
            Transform refChair = null;
            if (reference != null)
            {
                Vector3 p = reference.SitPoint != null ? reference.SitPoint.position : reference.transform.position;
                float best = float.MaxValue;
                foreach (var t in AllTransforms())
                {
                    if (!LooksLikeChair(t) || !HasMesh(t) || t.GetComponentInParent<SitSpot>() != null) continue;
                    if (!TopMost(t)) continue;
                    Bounds b = BoundsOf(t);
                    float d = b.SqrDistance(p);
                    if (d < best) { best = d; refChair = t; }
                }
                if (refChair != null && best > 1.0f) refChair = null;       // too far from any chair: do not guess
                if (refChair == null && reference.transform.parent != null && HasMesh(reference.transform.parent)) refChair = reference.transform.parent;
            }

            // leave out chairs that already have a seat
            var todo = new List<Transform>();
            foreach (var c in chairs)
            {
                if (c == refChair) continue;
                Vector3 centre = BoundsOf(c).center;
                bool has = false;
                foreach (var s in allSpots)
                {
                    Vector3 sp = s.SitPoint != null ? s.SitPoint.position : s.transform.position;
                    Vector2 a = new Vector2(sp.x, sp.z), b = new Vector2(centre.x, centre.z);
                    if (Vector2.Distance(a, b) < AlreadySeatedRadius && Mathf.Abs(sp.y - centre.y) < 1.5f) { has = true; break; }
                }
                if (!has && c.GetComponentInChildren<SitSpot>(true) == null) todo.Add(c);
            }

            if (todo.Count == 0)
            {
                EditorUtility.DisplayDialog("Sit spots", chairs.Count == 0
                    ? "No chairs found. Select the chairs in the Hierarchy (or name them \"...chair...\") and run this again."
                    : "Every chair already has a sit spot.", "OK");
                return;
            }

            string how = reference != null && refChair != null
                ? "copying the sit spot on \"" + refChair.name + "\""
                : "with a default seat (no sit spot on a chair to copy yet - place one first to control where and which way it faces)";
            if (!EditorUtility.DisplayDialog("Add sit spots",
                    "Add a sit spot to " + todo.Count + (fromSelection ? " selected object(s)" : " chair(s)") + ", " + how + "?\n\nYou can undo this with Ctrl+Z.", "Add", "Cancel"))
                return;

            // the prefab to instantiate
            GameObject prefab = null;
            if (reference != null) prefab = PrefabUtility.GetCorrespondingObjectFromSource(reference.gameObject);
            if (prefab == null) prefab = Resources.Load<GameObject>("CustomEditor/SitSpotPrefab");
            if (prefab == null) { Debug.LogError("[Seating] Could not find Resources/CustomEditor/SitSpotPrefab."); return; }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            var made = new List<Object>();
            Transform refCanvas = reference != null ? reference.GetComponentInChildren<Canvas>(true)?.transform : null;

            foreach (var chair in todo)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, chair.gameObject.scene);
                Undo.RegisterCreatedObjectUndo(go, "Add sit spot");
                go.name = "SittingPrefab";
                var spot = go.GetComponent<SitSpot>();

                Vector3 pos; Quaternion rot;
                if (reference != null && refChair != null)
                {
                    // same place on the chair as the one you set up
                    pos = chair.TransformPoint(refChair.InverseTransformPoint(reference.transform.position));
                    rot = chair.rotation * (Quaternion.Inverse(refChair.rotation) * reference.transform.rotation);
                }
                else
                {
                    // default: middle of the chair, at its base, facing the way the chair faces
                    Bounds b = BoundsOf(chair);
                    pos = new Vector3(b.center.x, b.min.y, b.center.z);
                    rot = Quaternion.Euler(0f, chair.eulerAngles.y, 0f);
                }
                go.transform.SetPositionAndRotation(pos, rot);
                go.transform.localScale = Vector3.one;

                // the SitPoint (what the player snaps to) and the button, as you set them on the first one
                if (spot != null && spot.SitPoint != null && reference != null && reference.SitPoint != null && reference.SitPoint != reference.transform)
                {
                    spot.SitPoint.localPosition = reference.SitPoint.localPosition;
                    spot.SitPoint.localRotation = reference.SitPoint.localRotation;
                }
                var canvas = go.GetComponentInChildren<Canvas>(true);
                if (canvas != null && refCanvas != null)
                {
                    canvas.transform.localPosition = refCanvas.localPosition;
                    canvas.transform.localRotation = refCanvas.localRotation;
                }

                // keep it with the chair when the chair has a normal scale; otherwise leave it loose in the scene (a scaled parent would squash the button)
                Vector3 ls = chair.lossyScale;
                if (Mathf.Abs(ls.x - 1f) < 0.02f && Mathf.Abs(ls.y - 1f) < 0.02f && Mathf.Abs(ls.z - 1f) < 0.02f)
                    go.transform.SetParent(chair, true);

                EditorSceneManager.MarkSceneDirty(go.scene);
                made.Add(go);
            }

            Undo.CollapseUndoOperations(group);
            Selection.objects = made.ToArray();
            Debug.Log("[Seating] Added " + made.Count + " sit spot(s). Check the arrow on each (it is the way the player faces when seated), then save the scene." +
                      " If the scene is an Addressable world, rebuild Addressables before testing.");
        }

        // ------------------------------------------------------------------ helpers

        static IEnumerable<GameObject> OpenRoots()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sc = SceneManager.GetSceneAt(i);
                if (!sc.isLoaded) continue;
                foreach (var r in sc.GetRootGameObjects()) yield return r;
            }
        }

        static IEnumerable<Transform> AllTransforms()
        {
            foreach (var r in OpenRoots())
                foreach (var t in r.GetComponentsInChildren<Transform>(true))
                    yield return t;
        }

        static bool LooksLikeChair(Transform t)
        {
            string n = t.name.ToLowerInvariant();
            if (n.StartsWith("sittingprefab") || n.StartsWith("sitspot") || n.StartsWith("sitpoint")) return false;
            return n.Contains("chair");
        }

        static bool HasMesh(Transform t)
        {
            return t.GetComponentInChildren<MeshRenderer>(true) != null || t.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;
        }

        /// <summary>True when no parent of this object is also a chair (so "Chair" counts, not its legs).</summary>
        static bool TopMost(Transform t)
        {
            for (var p = t.parent; p != null; p = p.parent)
                if (LooksLikeChair(p)) return false;
            return true;
        }

        static List<Transform> TopMost(List<Transform> list)
        {
            var set = new HashSet<Transform>(list);
            var result = new List<Transform>();
            foreach (var t in list)
            {
                bool inside = false;
                for (var p = t.parent; p != null; p = p.parent) if (set.Contains(p)) { inside = true; break; }
                if (!inside) result.Add(t);
            }
            return result;
        }

        static Bounds BoundsOf(Transform t)
        {
            bool have = false; Bounds b = new Bounds(t.position, Vector3.zero);
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                if (!have) { b = r.bounds; have = true; } else b.Encapsulate(r.bounds);
            }
            return b;
        }
    }
}
