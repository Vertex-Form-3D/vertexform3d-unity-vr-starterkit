using System.Collections;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;
using Photon.Realtime;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Content.Interaction;
using Unity.XR.CoreUtils;
using Photon.Voice.PUN;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using Photon.Voice.Unity;

namespace VertextFormCore
{
    public class PlayerNetworkSetup : MonoBehaviourPunCallbacks
    {
        public GameObject LocalXRRigGameobject;
        [SerializeField] private GameObject MainAvatarGameobject;

        [SerializeField] private GameObject AvatarHeadGameobject;
        [SerializeField] private GameObject AvatarBodyGameobject;

        [SerializeField] private GameObject[] AvatarModelPrefabs;
        [SerializeField] private TextMeshProUGUI PlayerName_Text;
        [SerializeField] private GameObject cameraOffset;
        [SerializeField] private XROrigin xROrigin;
        public float maxHeight;
        public float normalHeight;
        public TeleportationProvider tp;
        public ClimbProvider cp;
        [SerializeField] FlyingModeScript flyingScript;
        [SerializeField] InputActionManager IAM;
        public PhotonVoiceView voiceView;
        public Speaker speaker;
        public AudioListener audioListener;
        public Camera cam;
        public PlayerUIManager playerUIManager;
        public LocomotionManager locomotionManager;

        [SerializeField] GameObject[] nonSyncableObjects;

        void SetLayerRecursively(GameObject go, int layerNumber)
        {
            if (go == null) return;
            foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = layerNumber;
            }
        }

        [PunRPC]
        public void InitializeSelectedAvatarModel(int avatarSelectionNumber)
        {
            Debug.Log("-->on selected avatar " + avatarSelectionNumber + "for mine? " + photonView.IsMine);
            GameObject selectedAvatarGameobject = Instantiate(AvatarModelPrefabs[avatarSelectionNumber], LocalXRRigGameobject.transform);
            AvatarInputConverter avatarInputConverter = LocalXRRigGameobject.GetComponent<AvatarInputConverter>();
            AvatarHolder avatarHolder = selectedAvatarGameobject.GetComponent<AvatarHolder>();
            SetUpAvatarGameobject(avatarHolder.HeadTransform, avatarInputConverter.AvatarHead);
            SetUpAvatarGameobject(avatarHolder.BodyTransform, avatarInputConverter.AvatarBody);
            SetUpAvatarGameobject(avatarHolder.HandLeftTransform, avatarInputConverter.AvatarHand_Left);
            SetUpAvatarGameobject(avatarHolder.HandRightTransform, avatarInputConverter.AvatarHand_Right);

            if (!photonView.IsMine)
            {
                if (avatarInputConverter.AvatarHand_Left.GetComponentInChildren<AnimateHand>())
                {
                    Destroy(avatarInputConverter.AvatarHand_Left.GetComponentInChildren<AnimateHand>());
                }
                if (avatarInputConverter.AvatarHand_Right.GetComponentInChildren<AnimateHand>())
                {
                    Destroy(avatarInputConverter.AvatarHand_Right.GetComponentInChildren<AnimateHand>());
                }
            }
            else
            {
                avatarHolder.SetAvatarLayer();
            }
        }

        void SetUpAvatarGameobject(Transform avatarModelTransform, Transform mainAvatarTransform)
        {
            avatarModelTransform.SetParent(mainAvatarTransform);
            avatarModelTransform.localPosition = Vector3.zero;
            avatarModelTransform.localRotation = Quaternion.identity;
        }


        public IEnumerator Start()
        {
            while (!PhotonNetwork.InRoom)
            {
                yield return new WaitForSeconds(1);
            }
            if (audioListener != null)
            {
                Destroy(audioListener);
            }

            gameObject.name = $"player {photonView.Owner.NickName}";
            if (!SpawnManager.Instance.allPlayers.Contains(this))
            {
                SpawnManager.Instance.allPlayers.Add(this);
            }
            if (photonView.IsMine)
            {
                //The player is local
                LocalXRRigGameobject.SetActive(true);
                playerUIManager.InitializeSetting();

                //Getting the avatar selection data so that the correct avatar model can be instantiated.
                object avatarSelectionNumber;

                if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(MultiplayerVRConstants.AVATAR_SELECTION_NUMBER, out avatarSelectionNumber))
                {
                    photonView.RPC(nameof(InitializeSelectedAvatarModel), RpcTarget.AllBuffered, (int)avatarSelectionNumber);
                }

                SetLayerRecursively(AvatarHeadGameobject, 6);
                SetLayerRecursively(AvatarBodyGameobject, 7);


                if (SceneLoader.Instance.isFlyModeEnabled)
                {
                    
                }
                else
                {
                    if (flyingScript != null)
                    {
                        Destroy(flyingScript);
                    }
                }
                MainAvatarGameobject.AddComponent<AudioListener>();
            }
            else
            {
                cam.enabled = false;
                //The player is remote
                IAM.actionAssets.Clear();

                if (tp != null)
                {
                    Destroy(tp);
                }
                if (flyingScript != null)
                {
                    Destroy(flyingScript);
                }
                for (int i = 0; i < nonSyncableObjects.Length; i++)
                {
                    if (nonSyncableObjects[i].gameObject != null)
                    {
                        GameObject g = nonSyncableObjects[i].gameObject;
                        Destroy(g);
                    }
                }

                SetLayerRecursively(AvatarHeadGameobject, 0);
            }

            if (PlayerName_Text != null)
            {
                PlayerName_Text.text = photonView.Owner.NickName;
            }
        }

        private void OnDestroy()
        {
            if (SpawnManager.Instance.allPlayers.Contains(this))
            {
                SpawnManager.Instance.allPlayers.Remove(this);
            }
        }
        public void SetTeleportation()
        {
            if (photonView.IsMine)
            {
                TeleportationArea[] teleportationAreas = FindObjectsOfType<TeleportationArea>();
                if (teleportationAreas.Length > 0)
                {
                    Debug.Log("Found " + teleportationAreas.Length + " teleportation area. ");
                    foreach (var item in teleportationAreas)
                    {
                        item.teleportationProvider = tp;
                    }
                }
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"player Left {otherPlayer.NickName}");
        }

        public void CallSetStandingHeightRPC()
        {
            photonView.RPC(nameof(SetStandingHeight), RpcTarget.AllBuffered);
        }

        [PunRPC]
        public void SetStandingHeight()
        {
            cameraOffset.transform.localPosition = Vector3.up * maxHeight;
#if UNITY_EDITOR
            if (IsDeviceSimulatorActive())
            {
                xROrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.NotSpecified;
                cameraOffset.transform.localPosition = Vector3.up * 1.8f;
            }
#endif
        }

        public void CallSetSittingHeightRPC()
        {
            photonView.RPC(nameof(SetSittingHeight), RpcTarget.AllBuffered);
        }
        [PunRPC]
        public void SetSittingHeight()
        {
            cameraOffset.transform.localPosition = Vector3.up * normalHeight;
#if UNITY_EDITOR
            if (IsDeviceSimulatorActive())
            {
                xROrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.NotSpecified;
                cameraOffset.transform.localPosition = Vector3.up * 1.3644f;
            }
#endif
        }

        public void ResetPosition()
        {
            Debug.Log("Reset Position");
            transform.localPosition = Vector3.zero;
        }
        private bool IsDeviceSimulatorActive()
        {
            var ds = FindAnyObjectByType<XRDeviceSimulator>();
            if (ds != null)
            {
                return true;
            }
            return false;
        }

        public void MegaphoneHandler(bool active)
        {
            photonView.RPC(nameof(MegaPhoneRPCHandle), RpcTarget.AllBuffered,active, photonView.ViewID);
        }
        [PunRPC]
        public void MegaPhoneRPCHandle(bool on, int viewid)
        {
            if (photonView.ViewID == viewid)
            {
                speaker.GetComponent<AudioSource>().spatialBlend = on ? 0 : 1;
            }
        }
        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                HandleMasterClient();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == "Respawn")
            {
                ResetPosition();
            }
        }
        public void HandleMasterClient()
        {
            if (photonView.IsMine)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer.GetNext());
                }
            }
        }
    }
}