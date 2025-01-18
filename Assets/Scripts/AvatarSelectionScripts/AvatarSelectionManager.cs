using Photon.Pun;
using UnityEngine;

namespace VertextFormCore
{
    public class AvatarSelectionManager : MonoBehaviour
    {
        [SerializeField]
        GameObject AvatarSelectionPlatformGameobject;


        public GameObject[] selectableAvatarModels;
        public GameObject[] loadableAvatarModels;

        public int avatarSelectionNumber = 0;

        public AvatarInputConverter avatarInputConverter;


        /// <summary>
        /// Singleton Implementation
        /// </summary>
        public static AvatarSelectionManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            ////Initially, de-activating the Avatar Selection Platform.
            //AvatarSelectionPlatformGameobject.SetActive(false);

            object storedAvatarSelectionNumber;
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(MultiplayerVRConstants.AVATAR_SELECTION_NUMBER, out storedAvatarSelectionNumber))
            {
                Debug.Log("Stored avatar selection number: " + (int)storedAvatarSelectionNumber);
                avatarSelectionNumber = (int)storedAvatarSelectionNumber;
                ActivateAvatarModelAt(avatarSelectionNumber);
                LoadAvatarModelAt(avatarSelectionNumber);
            }
            else
            {
                avatarSelectionNumber = 0;
                ActivateAvatarModelAt(avatarSelectionNumber);
                LoadAvatarModelAt(avatarSelectionNumber);
            }


        }

        public void ActivateAvatarSelectionPlatform()
        {
            AvatarSelectionPlatformGameobject.SetActive(true);
        }

        public void DeactivateAvatarSelectionPlatform()
        {
            AvatarSelectionPlatformGameobject.SetActive(false);

        }

        public void NextAvatar()
        {
            avatarSelectionNumber += 1;
            if (avatarSelectionNumber >= selectableAvatarModels.Length)
            {
                avatarSelectionNumber = 0;
            }
            ActivateAvatarModelAt(avatarSelectionNumber);

        }

        public void PreviousAvatar()
        {
            avatarSelectionNumber -= 1;

            if (avatarSelectionNumber < 0)
            {
                avatarSelectionNumber = selectableAvatarModels.Length - 1;
            }
            ActivateAvatarModelAt(avatarSelectionNumber);

        }

        /// <summary>
        /// Activates the selected Avatar model inside the Avatar Selection Platform
        /// </summary>
        /// <param name="avatarIndex"></param>
        private void ActivateAvatarModelAt(int avatarIndex)
        {
            foreach (GameObject selectableAvatarModel in selectableAvatarModels)
            {
                selectableAvatarModel.SetActive(false);
            }

            selectableAvatarModels[avatarIndex].SetActive(true);
            Debug.Log(avatarSelectionNumber);

            LoadAvatarModelAt(avatarSelectionNumber);
        }

        /// <summary>
        /// Loads the Avatar Model and integrates into the VR Player Controller gameobject
        /// </summary>
        /// <param name="avatarIndex"></param>
        private void LoadAvatarModelAt(int avatarIndex)
        {
            foreach (GameObject loadableAvatarModel in loadableAvatarModels)
            {
                loadableAvatarModel.SetActive(false);
            }

            loadableAvatarModels[avatarIndex].SetActive(true);

            avatarInputConverter.MainAvatarTransform = loadableAvatarModels[avatarIndex].GetComponent<AvatarHolder>().MainAvatarTransform;

            avatarInputConverter.AvatarBody = loadableAvatarModels[avatarIndex].GetComponent<AvatarHolder>().BodyTransform;
            avatarInputConverter.AvatarHead = loadableAvatarModels[avatarIndex].GetComponent<AvatarHolder>().HeadTransform;
            avatarInputConverter.AvatarHand_Left = loadableAvatarModels[avatarIndex].GetComponent<AvatarHolder>().HandLeftTransform;
            avatarInputConverter.AvatarHand_Right = loadableAvatarModels[avatarIndex].GetComponent<AvatarHolder>().HandRightTransform;

            ExitGames.Client.Photon.Hashtable playerSelectionProp = new ExitGames.Client.Photon.Hashtable() { { MultiplayerVRConstants.AVATAR_SELECTION_NUMBER, avatarSelectionNumber } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerSelectionProp);
        }
    }
}