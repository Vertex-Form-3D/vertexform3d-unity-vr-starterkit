using System.Collections.Generic;
using TMPro;

namespace UnityEngine.XR.Hands.Samples.Gestures.DebugTools
{
    /// <summary>
    /// Controls the debug UI for bar states by scaling bars.
    /// </summary>
    public class XRFingerShapeDebugUI : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The list of 5 bars to display the finger state values.")]
        List<XRFingerShapeDebugBar> m_Bars = new List<XRFingerShapeDebugBar>();

        [SerializeField]
        [Tooltip("The label that will be used to display the name of the finger.")]
        TextMeshProUGUI m_FingerNameLabel;

        /// <summary>
        /// The list of 5 bars to display the finger state values
        /// </summary>
        public List<XRFingerShapeDebugBar> bars
        {
            get => m_Bars;
            set => m_Bars = value;
        }

        /// <summary>
        /// Set the value of a particular bar.
        /// </summary>
        /// <param name="barIndex">The bar that has the given state value.</param>
        /// <param name="value">The value for the given bar.</param>
        public void SetFingerShape(int barIndex, float value)
        {
            value = Mathf.Clamp(value, 0f, 1f);
            m_Bars[barIndex].SetValue(value);
        }

        /// <summary>
        /// Hides the bar for the given bar.
        /// </summary>
        /// <param name="barIndex">The bar that should have its bar hidden.</param>
        public void HideFingerShape(int barIndex) => m_Bars[barIndex].HideValue();

        /// <summary>
        /// Set the name of the finger for this finger state debug UI.
        /// </summary>
        /// <param name="fingerName">The string to put on the label.</param>
        public void SetFingerName(string fingerName)
        {
            if (m_FingerNameLabel != null)
                m_FingerNameLabel.text = fingerName;
        }
    }
}
