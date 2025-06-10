using System;
using TMPro;
using UnityEngine.UI;

namespace UnityEngine.XR.Hands.Samples.Gestures.DebugTools
{
    /// <summary>
    /// Controls the debug UI for a single <see cref="XRFingerState"/> that shows the value and optionally a target
    /// and range on the UI controlled.
    /// </summary>
    public class XRFingerShapeDebugBar : MonoBehaviour
    {
        const string k_InRangeText = "In range";
        const string k_OutOfRangeText = "Out of range";
        const string k_NoTargetSetText = "No Target Set";

        [SerializeField]
        [Tooltip("The container that determines the width of the max length bar, and holds the target and range indicators.")]
        RectTransform m_BarContainer;

        [SerializeField]
        [Tooltip("The bar that displays the value by being scaled in its local x axis from 0 to 1.")]
        Transform m_ValueBar;

        [SerializeField]
        [Tooltip("The target indicator that displays the target value by being positioned in its anchored x position from 0 to the bar container width.")]
        RectTransform m_TargetIndicator;

        [SerializeField]
        [Tooltip("The range indicator that displays the upper range by being positioned in its anchored x position from the range center and with the width of the range, proportional to the bar container.")]
        RectTransform m_UpperRangeIndicator;

        [SerializeField]
        [Tooltip("The range indicator that displays the lower range by being positioned in its anchored x position from the range center and with the width of the range, proportional to the bar container.")]
        RectTransform m_LowerRangeIndicator;

        [SerializeField]
        [Tooltip("The Image component that displays the upper range")]
        Image m_UpperRangeImage;

        [SerializeField]
        [Tooltip("The Image component that displays the lower range")]
        Image m_LowerRangeImage;

        [SerializeField]
        TextMeshProUGUI m_RangeStatusText;

        [SerializeField]
        [Tooltip("The Image component that displays the cross icon, denoting that input is within range for a detected gesture")]
        Image m_CheckmarkImage;

        [SerializeField]
        [Tooltip("The Image component that displays the cross icon, denoting that input is out of range for a detected gesture")]
        Image m_CrossImage;

        Color m_InRangeColor = Color.green;
        Color m_OutOfRangeColor = Color.red;
        Color m_NoSetTargetColor = new Color(0f, 0.627451f, 1f);

        float m_RangeRectHeight;

        Color m_RangeActiveColor;

        Color m_RangeDeactivatedColor;

        float m_TargetAmount;
        float m_UpperToleranceAmount;
        float m_LowerToleranceAmount;

        bool m_NoFingerShapeCondition;

        /// <summary>
        /// The container that determines the width of the max length bar, and holds the target and range indicators.
        /// </summary>
        public RectTransform barContainer
        {
            get => m_BarContainer;
            set => m_BarContainer = value;
        }

        /// <summary>
        /// The bar that displays the value by being scaled in its local x axis from <c>0</c> to <c>1</c>.
        /// </summary>
        public Transform valueBar
        {
            get => m_ValueBar;
            set => m_ValueBar = value;
        }

        /// <summary>
        /// The <see cref="RectTransform"/> that displays the target value by being positioned in its anchored x
        /// position from <c>0</c> to the <see cref="barContainer"/> width.
        /// </summary>
        public RectTransform targetIndicator
        {
            get => m_TargetIndicator;
            set => m_TargetIndicator = value;
        }

        /// <summary>
        /// The <see cref="RectTransform"/> that displays the upper range by being positioned in its anchored x position from
        /// the range center and with the width of the range, proportional to the <see cref="barContainer"/>.
        /// </summary>
        public RectTransform upperRangeIndicator
        {
            get => m_UpperRangeIndicator;
            set => m_UpperRangeIndicator = value;
        }

        /// <summary>
        /// The <see cref="RectTransform"/> that displays the lower range by being positioned in its anchored x position from
        /// the range center and with the width of the range, proportional to the <see cref="barContainer"/>.
        /// </summary>
        public RectTransform lowerRangeIndicator
        {
            get => m_LowerRangeIndicator;
            set => m_LowerRangeIndicator = value;
        }

        /// <summary>
        /// The Image component that displays the upper range
        /// </summary>
        public Image upperRangeImage
        {
            get => m_UpperRangeImage;
            set => m_UpperRangeImage = value;
        }

        /// <summary>
        /// The Image component that displays the lower range
        /// </summary>
        public Image lowerRangeImage
        {
            get => m_LowerRangeImage;
            set => m_LowerRangeImage = value;
        }

        /// <summary>
        /// The text component that displays the status of a tracked fingerstate being within a target range
        /// </summary>
        public TextMeshProUGUI rangeStatusText
        {
            get => m_RangeStatusText;
            set => m_RangeStatusText = value;
        }

        /// <summary>
        /// The Image component that displays the checkmark image when within the valid range
        /// </summary>
        public Image checkmarkImage
        {
            get => m_CheckmarkImage;
            set => m_CheckmarkImage = value;
        }

        /// <summary>
        /// The Image component that displays the cross-image when outside of the valid range
        /// </summary>
        public Image crossImage
        {
            get => m_CrossImage;
            set => m_CrossImage = value;
        }

        /// <summary>
        /// Change the appearance of the bar based on a handshape being detected
        /// </summary>
        public bool fingerShapeDetected
        {
            set
            {
                m_UpperRangeImage.color = value ? m_RangeActiveColor : m_RangeDeactivatedColor;
                m_LowerRangeImage.color = m_UpperRangeImage.color;

                var currentValue = m_ValueBar.localScale.x;
                var withinToleranceRange =
                    (m_TargetAmount + m_UpperToleranceAmount) > currentValue &&
                    (m_TargetAmount - m_LowerToleranceAmount) < currentValue;

                var positionIndent = m_RangeStatusText.transform.localPosition;
                if (value)
                {
                    positionIndent.x = 226f;

                    if (withinToleranceRange)
                    {
                        m_RangeStatusText.color = m_InRangeColor;
                        m_RangeStatusText.text = k_InRangeText;
                        m_UpperRangeImage.color = m_InRangeColor;
                        m_LowerRangeImage.color = m_InRangeColor;
                        m_CheckmarkImage.enabled = true;
                        m_CrossImage.enabled = false;
                    }
                    else
                    {
                        m_RangeStatusText.color = m_OutOfRangeColor;
                        m_RangeStatusText.text = k_OutOfRangeText;
                        m_UpperRangeImage.color = m_OutOfRangeColor;
                        m_LowerRangeImage.color = m_OutOfRangeColor;
                        m_CheckmarkImage.enabled = false;
                        m_CrossImage.enabled = true;
                    }
                }
                else
                {
                    positionIndent.x = 210f;

                    if (m_NoFingerShapeCondition)
                    {
                        m_RangeStatusText.text = k_NoTargetSetText;
                        m_RangeStatusText.color = m_NoSetTargetColor;

                        m_CheckmarkImage.enabled = false;
                        m_CrossImage.enabled = false;
                    }
                }

                m_RangeStatusText.transform.localPosition = positionIndent;
            }
        }

        void Awake()
        {
            m_RangeRectHeight = m_UpperRangeIndicator.rect.height;
            m_RangeActiveColor = m_UpperRangeImage.color;
            m_RangeDeactivatedColor = new Color(m_RangeActiveColor.r, m_RangeActiveColor.g, m_RangeActiveColor.b, 0.35f);
            fingerShapeDetected = false;

            m_CheckmarkImage.enabled = false;
            m_CrossImage.enabled = false;
        }

        /// <summary>
        /// Set the value to display on the bar. This scales the <see cref="valueBar"/> in the X direction.
        /// </summary>
        /// <param name="value">The normalized value to display on the bar.</param>
        public void SetValue(float value) => m_ValueBar.localScale = new Vector3(value, 1f, 1f);

        /// <summary>
        /// Hide the value display for the bar.
        /// </summary>
        public void HideValue() => SetValue(0f);

        /// <summary>
        /// Set the target value and tolerance to display on the bar. The tolerance is converted to a range that centers
        /// on the target and extends out in both directions by the tolerance.
        /// </summary>
        /// <param name="target">The normalized target value.</param>
        /// <param name="upperTolerance">The upper tolerance to show around the target value.</param>
        /// <param name="lowerTolerance">The lower tolerance to show around the target value.</param>
        public void SetTargetAndTolerances(float target, float upperTolerance, float lowerTolerance)
        {
            m_TargetAmount = target;
            m_UpperToleranceAmount = upperTolerance;
            m_LowerToleranceAmount = lowerTolerance;

            m_TargetIndicator.gameObject.SetActive(true);
            m_UpperRangeIndicator.gameObject.SetActive(true);
            m_LowerRangeIndicator.gameObject.SetActive(true);
            var containerWidth = m_BarContainer.rect.width;
            m_TargetIndicator.anchoredPosition = new Vector3(target * containerWidth, 0f, 0f);
            m_UpperRangeIndicator.anchoredPosition = new Vector2(target * containerWidth, 0f);
            m_UpperRangeIndicator.sizeDelta = new Vector2(upperTolerance * containerWidth, m_RangeRectHeight);
            m_LowerRangeIndicator.anchoredPosition = new Vector2(target * containerWidth - 100f, 0f);
            m_LowerRangeIndicator.sizeDelta = new Vector2(lowerTolerance * containerWidth, m_RangeRectHeight);

            fingerShapeDetected = true;
        }

        /// <summary>
        /// Hide the target and tolerance range for the bar.
        /// </summary>
        public void HideTargetAndTolerance()
        {
            m_NoFingerShapeCondition = true;

            m_TargetIndicator.gameObject.SetActive(false);
            m_UpperRangeIndicator.gameObject.SetActive(false);
            m_LowerRangeIndicator.gameObject.SetActive(false);

            m_RangeStatusText.text = k_NoTargetSetText;
            m_RangeStatusText.color = m_NoSetTargetColor;
        }
    }
}
