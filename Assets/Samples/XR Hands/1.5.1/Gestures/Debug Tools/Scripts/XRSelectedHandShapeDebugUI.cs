using TMPro;
using UnityEngine.XR.Hands.Gestures;

namespace UnityEngine.XR.Hands.Samples.Gestures.DebugTools
{
    /// <summary>
    /// Updates the text label that denotes the currently detected hand gesture
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class XRSelectedHandShapeDebugUI : MonoBehaviour
    {
        /// <summary>
        /// The string displayed when no gesture is detected
        /// </summary>
        const string k_NoGestureDetectedString = "None Detected";

        [SerializeField]
        [Tooltip("The label that will be used to display the name of the hand shape.")]
        TextMeshProUGUI m_HandShapeNameLabel;

        /// <summary>
        /// The text label that denotes and displays the currently detected hand gesture
        /// </summary>
        public TextMeshProUGUI handShapeNameLabel
        {
            get => m_HandShapeNameLabel;
            set => m_HandShapeNameLabel = value;
        }

        void Awake()
        {
            m_HandShapeNameLabel.text = k_NoGestureDetectedString;
        }

        /// <summary>
        /// Update the text label that denotes the currently detected hand gesture
        /// </summary>
        /// <param name="handPoseOrShape">The pose or shape whose name will be displayed</param>
        public void UpdateSelectedHandshapeTextUI(ScriptableObject handPoseOrShape)
        {
            var handShape = handPoseOrShape as XRHandShape;
            var handPose = handPoseOrShape as XRHandPose;

            if (handShape)
                m_HandShapeNameLabel.text = handShape.name;
            else if (handPose)
                m_HandShapeNameLabel.text = handPose.name;
        }

        /// <summary>
        /// Update the text label that denote that no hand gesture is currently detected
        /// </summary>
        public void ResetUI()
        {
            m_HandShapeNameLabel.text = k_NoGestureDetectedString;
        }
    }
}
