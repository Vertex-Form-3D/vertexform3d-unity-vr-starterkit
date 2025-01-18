using UnityEngine;

namespace VertextFormCore
{
    public class TeleportXROrigin : MonoBehaviour
    {
        public Transform teleportDestination;

        private void OnTriggerEnter(Collider other)
        {
            // Check if the collider belongs to the XR Origin or a player controller.
            if (other.CompareTag("Player")) // Make sure your XR Origin or its components have the "Player" tag.
            {
                // Assuming your XR Origin is structured with the XR Rig at the root,
                // and teleportDestination is the Transform where you want to teleport the XR Origin.
                TeleportPlayer(other);
            }
        }

        private void TeleportPlayer(Collider playerCollider)
        {
            // Find the XR Rig component. You might need to adjust this depending on your project's structure.
            TeleportXROrigin rig = FindObjectOfType<TeleportXROrigin>();

            if (rig == null)
            {
                Debug.LogError("TeleportXROrigin not found in the scene.");
                return;
            }

            // Calculate the difference between the player's current position and the teleportation destination,
            // then apply this difference to the XR Rig's position.
            Vector3 difference = teleportDestination.position - playerCollider.transform.position;
            rig.transform.position += difference;
        }
    }
}