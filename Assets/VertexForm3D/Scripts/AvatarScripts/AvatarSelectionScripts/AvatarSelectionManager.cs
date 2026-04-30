using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace VertexFormCore
{
    public class AvatarSelectionManager : MonoBehaviour
    {
        [SerializeField]
        GameObject AvatarSelectionPlatformGameobject;

        public Button previousButton;
        public Button nextButton;

        [Header("Custom Avatar Properties")]
        public Transform headTransform;
        public Transform bodyTransform;
        public Transform leftHandTransform;
        public Transform rightHandTransform;
        public Transform headParent;
        public Transform bodyParent;
        public Transform leftHandParent;
        public Transform rightHandParent;
        public GameObject customAvatarSelection;
        public GameObject customAvatarSelectionUI;
        public AvatarHolder customavatarloader;
        [Header("Custom Avatar Properties")]
        public XRInputModalityManager XRIMM;
        public int avatarSelectionNumber = 0;
        public Renderer leftHandVisual;
        public Renderer rightHandVisual;
        public GameObject bottomButtonParent;
        public GameObject avatarSelectionCanvas;
        public GameObject mainUICanvas;
        public GameObject mainMenuCanvas;
        public AvatarInputConverter avatarInputConverter;


        /// <summary>
        /// Singleton Implementation
        /// </summary>
        public static AvatarSelectionManager Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }


        public void OnTapChangeAvatar()
        {
            bottomButtonParent.SetActive(false);
            mainUICanvas.SetActive(false);
            mainMenuCanvas.SetActive(false);
            avatarSelectionCanvas.SetActive(true);
            OnTapCustomAvatarSelection();
        }

        public void OnCloseAvatarSelection()
        {
            bottomButtonParent.SetActive(true);
            mainUICanvas.SetActive(true);
            mainMenuCanvas.SetActive(true);
            avatarSelectionCanvas.SetActive(false);
        }

        private void Start()
        {
            ////Initially, de-activating the Avatar Selection Platform.
            //AvatarSelectionPlatformGameobject.SetActive(false);
            previousButton.onClick.AddListener(() =>
            {
                PreviousAvatar();
            });

            nextButton.onClick.AddListener(() =>
            {
                NextAvatar();
            });

            InitializeAvatarSystem();
            HandAndControllerSync();
        }

        private void HandAndControllerSync()
        {
            XRIMM.trackedHandModeStarted.AddListener(OnTrackedHandModeStarted);
            XRIMM.trackedHandModeEnded.AddListener(OnTrackedHandModeEnded);
            XRIMM.motionControllerModeStarted.AddListener(OnMotionControllerModeStarted);
            XRIMM.motionControllerModeEnded.AddListener(OnMotionControllerModeEnded);
            if (XRIMM.leftController.activeInHierarchy || XRIMM.rightController.activeInHierarchy)
            {
                OnMotionControllerModeStarted();
            }
            else
            {
                OnTrackedHandModeStarted();
            }
        }


        private void OnMotionControllerModeStarted()
        {
            leftHandParent.gameObject.SetActive(true);
            rightHandParent.gameObject.SetActive(true);
        }

        private void OnMotionControllerModeEnded()
        {

        }

        private void OnTrackedHandModeEnded()
        {

        }

        private void OnTrackedHandModeStarted()
        {
            leftHandVisual.enabled = rightHandVisual.enabled = true;

            leftHandParent.gameObject.SetActive(false);
            rightHandParent.gameObject.SetActive(false);
        }

        public void InitializeAvatarSystem()
        {
            OnTapCustomAvatarSelection();

        }
        public void OnTapCustomAvatarSelection()
        {
            avatarSelectionNumber = PlayerPrefs.GetInt(MultiplayerVRConstants.AVATAR_SELECTION_NUMBER);
            ActivateAvatarModelAt(avatarSelectionNumber);
            customAvatarSelection.SetActive(true);
            customAvatarSelectionUI.SetActive(true);
            if (customAvatarSelectionUI.activeInHierarchy)
            {
                leftHandVisual.enabled = rightHandVisual.enabled = true;
            }
            else
            {
                leftHandVisual.enabled = rightHandVisual.enabled = false;
            }
            leftHandParent.gameObject.SetActive(XRIMM.leftController.activeInHierarchy);
            rightHandParent.gameObject.SetActive(XRIMM.rightController.activeInHierarchy);
        }

        public void DeactivateAvatarSelectionPlatform()
        {
            AvatarSelectionPlatformGameobject.SetActive(false);

        }

        public void NextAvatar()
        {
            avatarSelectionNumber += 1;
            if (avatarSelectionNumber >= ProjectManager.instance.uiLayoutConfig.avatarDatas.Count)
            {
                avatarSelectionNumber = 0;
            }

            PlayerPrefs.SetInt(MultiplayerVRConstants.AVATAR_SELECTION_NUMBER, avatarSelectionNumber);
            PlayerPrefs.SetString("IS_RPM", "false");
            ActivateAvatarModelAt(avatarSelectionNumber);

        }

        public void PreviousAvatar()
        {
            avatarSelectionNumber -= 1;

            if (avatarSelectionNumber < 0)
            {
                avatarSelectionNumber = ProjectManager.instance.uiLayoutConfig.avatarDatas.Count - 1;
            }
            PlayerPrefs.SetInt(MultiplayerVRConstants.AVATAR_SELECTION_NUMBER, avatarSelectionNumber);
            PlayerPrefs.SetString("IS_RPM", "false");
            ActivateAvatarModelAt(avatarSelectionNumber);

        }

        /// <summary>
        /// Activates the selected Avatar model inside the Avatar Selection Platform
        /// </summary>
        /// <param name="avatarIndex"></param>
        private void ActivateAvatarModelAt(int avatarIndex)
        {
            ClearChildren(headParent);
            ClearChildren(headTransform);
            ClearChildren(bodyTransform);
            ClearChildren(bodyParent);
            GameObject body = Instantiate(ProjectManager.instance.uiLayoutConfig.avatarDatas[avatarIndex].body);
            body.transform.SetParent(bodyParent, false);
            GameObject head = Instantiate(ProjectManager.instance.uiLayoutConfig.avatarDatas[avatarIndex].head);
            head.transform.SetParent(headParent, false);

            GameObject body1 = Instantiate(ProjectManager.instance.uiLayoutConfig.avatarDatas[avatarIndex].body);
            body1.transform.SetParent(bodyTransform, false);
            GameObject head1 = Instantiate(ProjectManager.instance.uiLayoutConfig.avatarDatas[avatarIndex].head);
            head1.transform.SetParent(headTransform, false);
            body.transform.localPosition = body1.transform.localPosition = head.transform.localPosition = head1.transform.localPosition = Vector3.zero;
            customavatarloader.SetAvatar(head1, body1, true);
        }

        void ClearChildren(Transform tr)
        {
            foreach (Transform child in tr)
            {
                Destroy(child.gameObject);
            }
        }
    }
}