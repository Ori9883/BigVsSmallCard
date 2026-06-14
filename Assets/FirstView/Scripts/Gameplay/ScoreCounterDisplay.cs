using UnityEngine;
using TMPro;
using DG.Tweening;

namespace FirstView.Gameplay
{
    /// <summary>
    /// Displays an integer score on a TextMeshPro label with a count-up
    /// roll animation and a scale punch whenever the value increases.
    /// Place on the Counter GameObject (parent of the Canvas/TMP hierarchy).
    /// </summary>
    public class ScoreCounterDisplay : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("TMP label that shows the score. Auto-found via GetComponentInChildren if left empty.")]
        [SerializeField] private TextMeshProUGUI label;

        [Header("Count-Up Animation")]
        [SerializeField] private float countDuration = 0.8f;
        [SerializeField] private Ease countEase = Ease.OutQuad;

        [Header("Punch (Pop) Animation")]
        [SerializeField] private float punchScale = 0.3f;
        [SerializeField] private float punchDuration = 0.5f;
        [SerializeField] private int punchVibrato = 6;
        [SerializeField] private float punchElasticity = 1f;

        private int displayedValue;
        private float animatedValue;
        private Sequence activeSequence;

        private void Awake()
        {
            if (label == null)
                label = GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = "0";
        }

        /// <summary>Sets the score instantly without animation.</summary>
        public void SetValueImmediate(int value)
        {
            KillActive();
            displayedValue = value;
            animatedValue = value;
            if (label != null)
                label.text = value.ToString();
        }

        /// <summary>
        /// Animates the displayed number from its current value to
        /// <paramref name="targetValue"/> with a roll-up tween and a
        /// scale punch on the label.
        /// </summary>
        public void AnimateTo(int targetValue)
        {
            KillActive();

            int from = displayedValue;
            displayedValue = targetValue;

            // No change → just snap the text.
            if (from == targetValue)
            {
                animatedValue = targetValue;
                if (label != null)
                    label.text = targetValue.ToString();
                return;
            }

            animatedValue = from;

            activeSequence = DOTween.Sequence();

            // Number roll-up: interpolate the displayed integer.
            activeSequence.Append(
                DOTween.To(
                    () => animatedValue,
                    v =>
                    {
                        animatedValue = v;
                        if (label != null)
                            label.text = Mathf.RoundToInt(v).ToString();
                    },
                    (float)targetValue,
                    countDuration)
                .SetEase(countEase));

            // Scale punch for a satisfying "pop".
            if (label != null)
            {
                activeSequence.Join(
                    label.transform.DOPunchScale(
                        Vector3.one * punchScale,
                        punchDuration,
                        punchVibrato,
                        punchElasticity));
            }

            activeSequence.OnKill(() => activeSequence = null);
        }

        private void KillActive()
        {
            if (activeSequence != null)
            {
                activeSequence.Kill(true); // complete → snaps to end values
                activeSequence = null;
            }
        }

        private void OnDestroy()
        {
            KillActive();
        }
    }
}
