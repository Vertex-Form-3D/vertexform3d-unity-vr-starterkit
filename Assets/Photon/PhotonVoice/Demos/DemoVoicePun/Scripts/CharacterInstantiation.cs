// ----------------------------------------------------------------------------
// <copyright file="CharacterInstantiation.cs" company="Exit Games GmbH">
// Photon Voice Demo for PUN- Copyright (C) 2016 Exit Games GmbH
// </copyright>
// <summary>
// Class that handles character instantiation when the actor is joined.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

namespace ExitGames.Demos.DemoPunVoice
{
    using System.Collections.Generic;
    using ExitGames.Client.Photon;
    using Photon.Realtime;
    using UnityEngine;
    using Photon.Pun;

    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    public class CharacterInstantiation : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        public enum SpawnSequence { Connection, Random, RoundRobin }

        public Transform SpawnPosition;
        public float PositionOffset = 2.0f;
        public GameObject[] PrefabsToInstantiate;
        public List<Transform> SpawnPoints;

        public bool AutoSpawn = true;
        public bool UseRandomOffset = true;
        public SpawnSequence Sequence = SpawnSequence.Connection;

        public delegate void OnCharacterInstantiated(GameObject character);
        public static event OnCharacterInstantiated CharacterInstantiated;

        [SerializeField] 
        private byte manualInstantiationEventCode = 1;

        protected int lastUsedSpawnPointIndex = -1;

        #pragma warning disable 649
        [SerializeField]
        private bool manualInstantiation;

        [SerializeField] 
        private bool differentPrefabs;

        [SerializeField] private string localPrefabSuffix;
        [SerializeField] private string remotePrefabSuffix;
        #pragma warning restore 649

        public override void OnJoinedRoom()
        {
            if (!this.AutoSpawn)
            {
                return;
            }
            if (this.PrefabsToInstantiate != null)
            {
                int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
                if (actorNumber < 1)
                {
                    actorNumber = 1;
                }
                int index = (actorNumber - 1) % this.PrefabsToInstantiate.Length;
                Vector3 spawnPos;
                Quaternion spawnRotation;
                this.GetSpawnPoint(out spawnPos, out spawnRotation);
                Camera.main.transform.position += spawnPos;

                if (this.manualInstantiation)
                {
                    this.ManualInstantiation(index, spawnPos, spawnRotation);
                }
                else
                {
                    GameObject o = this.PrefabsToInstantiate[index];
                    o = PhotonNetwork.Instantiate(o.name, spawnPos, spawnRotation);
                    if (CharacterInstantiated != null)
                    {
                        CharacterInstantiated(o);
                    }
                }
            }
        }

        private void ManualInstantiation(int index, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = this.PrefabsToInstantiate[index];
            GameObject player;
            if (this.differentPrefabs)
            {
                player = Instantiate(Resources.Load(string.Format("{0}{1}", prefab.name, this.localPrefabSuffix)) as GameObject, position, rotation);
            }
            else
            {
                player = Instantiate(prefab, position, rotation);
            }
            PhotonView photonView = player.GetComponent<PhotonView>();

            if (PhotonNetwork.AllocateViewID(photonView))
            {
                object[] data =
                {
                    index, player.transform.position, player.transform.rotation, photonView.ViewID
                };

                RaiseEventOptions raiseEventOptions = new RaiseEventOptions
                {
                    Receivers = ReceiverGroup.Others,
                    CachingOption = EventCaching.AddToRoomCache
                };

                PhotonNetwork.RaiseEvent(this.manualInstantiationEventCode, data, raiseEventOptions, SendOptions.SendReliable);
                if (CharacterInstantiated != null)
                {
                    CharacterInstantiated(player);
                }
            }
            else
            {
                Debug.LogError("Failed to allocate a ViewId.");

                Destroy(player);
            }
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code == this.manualInstantiationEventCode)
            {
                object[] data = photonEvent.CustomData as object[];
                int prefabIndex = (int) data[0];
                GameObject prefab = this.PrefabsToInstantiate[prefabIndex];
                Vector3 position = (Vector3)data[1];
                Quaternion rotation = (Quaternion)data[2];
                GameObject player;
                if (this.differentPrefabs)
                {
                    player = Instantiate(Resources.Load(string.Format("{0}{1}", prefab.name, this.remotePrefabSuffix)) as GameObject, position, rotation);
                }
                else
                {
                    player = Instantiate(prefab, position, Quaternion.identity);
                }
                PhotonView photonView = player.GetComponent<PhotonView>();
                photonView.ViewID = (int) data[3];
            }
        }

        #if UNITY_EDITOR

        protected void OnValidate()
        {
            // Move any values from old SpawnPosition field to new SpawnPositions
            if (this.SpawnPosition)
            {
                if (this.SpawnPoints == null)
                    this.SpawnPoints = new List<Transform>();

                this.SpawnPoints.Add(this.SpawnPosition);
                this.SpawnPosition = null;
            }
        }
        
        #endif

        /// <summary>
        /// Override this method with any custom code for coming up with a spawn location.
        /// </summary>
        protected virtual void GetSpawnPoint(out Vector3 spawnPos, out Quaternion spawnRot)
        {
            // Fetch a point using the Sequence method indicated
            Transform point = this.GetSpawnPoint();
            if (point != null)
            {
                spawnPos = point.position;
                spawnRot = point.rotation;
            }
            else
            {
                spawnPos = new Vector3(0, 0, 0);
                spawnRot = new Quaternion(0, 0, 0, 1);
            }
            if (this.UseRandomOffset)
            {
                Random.InitState((int)(Time.time * 10000));
                Vector3 random = Random.insideUnitSphere;
                random.y = 0;
                random = random.normalized;
                spawnPos += this.PositionOffset * random;
            }
        }

        protected virtual Transform GetSpawnPoint()
        {
            // Fetch a point using the Sequence method indicated
            if (this.SpawnPoints == null || this.SpawnPoints.Count == 0)
            {
                return null;
            }
            switch (this.Sequence)
            {
                case SpawnSequence.Connection:
                {
                    int id = PhotonNetwork.LocalPlayer.ActorNumber;
                    return this.SpawnPoints[(id == -1) ? 0 : id % this.SpawnPoints.Count];
                }

                case SpawnSequence.RoundRobin:
                {
                    this.lastUsedSpawnPointIndex++;
                    if (this.lastUsedSpawnPointIndex >= this.SpawnPoints.Count)
                    {
                        this.lastUsedSpawnPointIndex = 0;
                    }

                    // Use Vector.Zero and Quaternion.Identity if we are dealing with no or a null spawnpoint.
                    return this.SpawnPoints[this.lastUsedSpawnPointIndex];
                }

                case SpawnSequence.Random:
                {
                    return this.SpawnPoints[Random.Range(0, this.SpawnPoints.Count)];
                }

                default:
                    return null;
            }
        }
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(CharacterInstantiation))]
    public class CharacterInstantiationEditor : Editor
    {
        private SerializedProperty spawnPoints, prefabsToInstantiate, useRandomOffset, positionOffset, autoSpawn, manualInstantiation, differentPrefabs, localPrefabSuffix, remotePrefabSuffix, sequence, manualInstantiationEventCode;
        private GUIStyle fieldBox;
        private const int PAD = 6;

        private void OnEnable()
        {
            this.spawnPoints = this.serializedObject.FindProperty("SpawnPoints");
            this.prefabsToInstantiate = this.serializedObject.FindProperty("PrefabsToInstantiate");
            this.useRandomOffset = this.serializedObject.FindProperty("UseRandomOffset");
            this.positionOffset = this.serializedObject.FindProperty("PositionOffset");
            this.autoSpawn = this.serializedObject.FindProperty("AutoSpawn");
            this.manualInstantiation = this.serializedObject.FindProperty("manualInstantiation");
            this.differentPrefabs = this.serializedObject.FindProperty("differentPrefabs");
            this.localPrefabSuffix = this.serializedObject.FindProperty("localPrefabSuffix");
            this.remotePrefabSuffix = this.serializedObject.FindProperty("remotePrefabSuffix");
            this.manualInstantiationEventCode = this.serializedObject.FindProperty("manualInstantiationEventCode");
            this.sequence = this.serializedObject.FindProperty("Sequence");
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            this.EditableReferenceList(this.prefabsToInstantiate, new GUIContent(this.prefabsToInstantiate.displayName, this.prefabsToInstantiate.tooltip), this.fieldBox);

            this.EditableReferenceList(this.spawnPoints, new GUIContent(this.spawnPoints.displayName, this.spawnPoints.tooltip), this.fieldBox);

            if (this.fieldBox == null)
            {
                this.fieldBox = new GUIStyle("HelpBox") { padding = new RectOffset(PAD, PAD, PAD, PAD) };
            }

            // Spawn Pattern
            EditorGUILayout.LabelField("Spawn Pattern Setup");
            EditorGUILayout.BeginVertical(this.fieldBox);
            EditorGUILayout.PropertyField(this.sequence);
            EditorGUILayout.PropertyField(this.useRandomOffset);
            if (this.useRandomOffset.boolValue)
            {
                EditorGUILayout.PropertyField(this.positionOffset);
            }
            EditorGUILayout.EndVertical();

            // Auto/Manual Spawn
            EditorGUILayout.LabelField("Network Instantiation Setup");
            EditorGUILayout.BeginVertical(this.fieldBox);
            EditorGUILayout.PropertyField(this.autoSpawn);
            EditorGUILayout.PropertyField(this.manualInstantiation);
            if (this.manualInstantiation.boolValue)
            {
                EditorGUILayout.PropertyField(this.manualInstantiationEventCode);
                EditorGUILayout.PropertyField(this.differentPrefabs);
                if (this.differentPrefabs.boolValue)
                {
                    EditorGUILayout.PropertyField(this.localPrefabSuffix);
                    EditorGUILayout.PropertyField(this.remotePrefabSuffix);
                }
            }
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                this.serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Create a basic rendered list of objects from a SerializedProperty list or array, with Add/Destroy buttons.
        /// </summary>
        public void EditableReferenceList(SerializedProperty list, GUIContent gc, GUIStyle style = null)
        {
            EditorGUILayout.LabelField(gc);

            if (style == null)
                style = new GUIStyle("HelpBox") { padding = new RectOffset(6, 6, 6, 6) };

            EditorGUILayout.BeginVertical(style);

            int count = list.arraySize;

            if (count == 0)
            {
                if (GUI.Button(EditorGUILayout.GetControlRect(GUILayout.MaxWidth(20)), "+", (GUIStyle)"minibutton"))
                {
                    list.InsertArrayElementAtIndex(0);
                    list.GetArrayElementAtIndex(0).objectReferenceValue = null;
                }
            }
            else
            {
                // List Elements and Delete buttons
                for (int i = 0; i < list.arraySize; ++i)
                {
                    EditorGUILayout.BeginHorizontal();
                    bool add = (GUI.Button(EditorGUILayout.GetControlRect(GUILayout.MaxWidth(20)), "+", (GUIStyle)"minibutton"));
                    EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), GUIContent.none);
                    bool remove = (GUI.Button(EditorGUILayout.GetControlRect(GUILayout.MaxWidth(20)), "x", (GUIStyle)"minibutton"));

                    EditorGUILayout.EndHorizontal();

                    if (add)
                    {
                        list.InsertArrayElementAtIndex(i);
                        list.GetArrayElementAtIndex(i).objectReferenceValue = null;
                        EditorGUILayout.EndHorizontal();
                        break;
                    }

                    if (remove)
                    {
                        list.DeleteArrayElementAtIndex(i);
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                }
            }
                
            EditorGUILayout.EndVertical();
        }
    }
    #endif
}