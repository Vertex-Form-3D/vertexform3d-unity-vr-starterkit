using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace VertexFormCore
{
    [System.Serializable]
    public class NetworkSpawnableObject
    {
        public NetworkObject prefab;
        public Transform transform;
    }

    public class NetworkObjectSpawner : MonoBehaviour
    {
        public List<NetworkSpawnableObject> objectsToSpawn = new List<NetworkSpawnableObject>();

        // Call this from the host after joining a session
        public void SpawnAll(NetworkRunner runner)
        {
            foreach (var obj in objectsToSpawn)
            {
                runner.Spawn(obj.prefab, obj.transform.position, obj.transform.rotation);
            }
        }
    }
}