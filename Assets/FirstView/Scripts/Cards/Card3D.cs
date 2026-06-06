using UnityEngine;
using System.Collections;

namespace FirstView
{
    /// <summary>
    /// A 3D card sitting on the table.
    /// Supports hover-lift, draw, place, and flip animations.
    /// Card face info is rendered via a child WorldSpace Canvas.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Card3D : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private MeshRenderer cardMeshRenderer;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private GameObject infoPanel;

        [Header("Card Data")]
        public string cardId;
        public string cardName = "Card";
        public int attack;
        public int health;
        public string ability = "";
        public int cost;
        public CardRarity rarity = CardRarity.Common;

        [Header("Animation")]
        [SerializeField] private float hoverLiftHeight = 0.12f;
        [SerializeField] private float hoverTiltAngle = 8f;
        [SerializeField] private float animSmoothTime = 0.12f;

        private Vector3 basePosition;
        private Quaternion baseRotation;
        private bool isHovered;
        private Coroutine activeAnim;

        public bool IsHovered => isHovered;
        public Vector3 BasePosition => basePosition;

        private void Awake()
        {
            if (cardMeshRenderer == null) cardMeshRenderer = GetComponentInChildren<MeshRenderer>();
            if (visualRoot == null) visualRoot = transform;

            var collider = GetComponent<BoxCollider>();
            collider.size = new Vector3(0.52f, 0.001f, 0.72f);
            collider.center = new Vector3(0f, 0.001f, 0f);

            if (infoPanel != null) infoPanel.SetActive(false);
        }

        public void SetBasePose(Vector3 position, Quaternion rotation)
        {
            basePosition = position;
            baseRotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
        }

        public void SetHover(bool hovered)
        {
            if (isHovered == hovered) return;
            isHovered = hovered;

            if (infoPanel != null) infoPanel.SetActive(hovered);

            if (activeAnim != null) StopCoroutine(activeAnim);
            activeAnim = StartCoroutine(AnimateHover(hovered));
        }

        public void PlayDrawAnimation(Vector3 fromPos, Quaternion fromRot, float delay = 0f)
        {
            if (activeAnim != null) StopCoroutine(activeAnim);
            activeAnim = StartCoroutine(DrawSequence(fromPos, fromRot, delay));
        }

        public void PlayPlaceAnimation(Vector3 targetPos, Quaternion targetRot)
        {
            if (activeAnim != null) StopCoroutine(activeAnim);
            activeAnim = StartCoroutine(PlaceSequence(targetPos, targetRot));
        }

        public void PlayAttackAnimation()
        {
            if (activeAnim != null) StopCoroutine(activeAnim);
            activeAnim = StartCoroutine(AttackSequence());
        }

        public void PlayDeathAnimation()
        {
            if (activeAnim != null) StopCoroutine(activeAnim);
            activeAnim = StartCoroutine(DeathSequence());
        }

        public void SetCardBackVisible(bool showBack)
        {
            // Flip the visual root 180 degrees on Y if showing back
            Vector3 euler = visualRoot.localEulerAngles;
            euler.y = showBack ? 180f : 0f;
            visualRoot.localEulerAngles = euler;
        }

        public void SetEmissionGlow(bool on, Color? color = null)
        {
            if (cardMeshRenderer == null) return;
            foreach (var mat in cardMeshRenderer.materials)
            {
                if (on)
                {
                    mat.EnableKeyword("_EMISSION");
                    if (color.HasValue) mat.SetColor("_EmissionColor", color.Value);
                    else mat.SetColor("_EmissionColor", new Color(0.3f, 0.25f, 0.1f));
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                }
            }
        }

        #region Animation Coroutines

        private IEnumerator AnimateHover(bool lift)
        {
            Vector3 targetPos = lift ? basePosition + Vector3.up * hoverLiftHeight : basePosition;
            Quaternion targetRot = lift
                ? Quaternion.Euler(baseRotation.eulerAngles.x - hoverTiltAngle, baseRotation.eulerAngles.y, 0f)
                : baseRotation;

            yield return SmoothTransform(targetPos, targetRot, animSmoothTime);
        }

        private IEnumerator DrawSequence(Vector3 fromPos, Quaternion fromRot, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            transform.SetPositionAndRotation(fromPos, fromRot);
            float duration = 0.55f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = t * t * (3f - 2f * t);

                Vector3 pos = Vector3.Lerp(fromPos, basePosition, ease);
                pos.y += Mathf.Sin(ease * Mathf.PI) * 0.25f;
                transform.position = pos;

                float flip = Mathf.Lerp(180f, 0f, ease);
                transform.rotation = baseRotation * Quaternion.Euler(0f, flip, 0f);
                yield return null;
            }

            transform.SetPositionAndRotation(basePosition, baseRotation);
            activeAnim = null;
        }

        private IEnumerator PlaceSequence(Vector3 targetPos, Quaternion targetRot)
        {
            basePosition = targetPos;
            baseRotation = targetRot;

            float duration = 0.35f;
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = t * t * (3f - 2f * t);

                Vector3 pos = Vector3.Lerp(startPos, targetPos, ease);
                pos.y += Mathf.Sin(ease * Mathf.PI) * 0.15f;
                transform.SetPositionAndRotation(pos, Quaternion.Slerp(startRot, targetRot, ease));
                yield return null;
            }

            transform.SetPositionAndRotation(targetPos, targetRot);
            activeAnim = null;
        }

        private IEnumerator AttackSequence()
        {
            Vector3 fwd = transform.forward * 0.15f;
            float duration = 0.25f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float punch = Mathf.Sin(t * Mathf.PI);
                transform.position = basePosition + fwd * punch;
                yield return null;
            }

            transform.position = basePosition;
            activeAnim = null;
        }

        private IEnumerator DeathSequence()
        {
            float duration = 0.8f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Shrink and sink
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
                Vector3 sinkPos = basePosition;
                sinkPos.y -= t * 0.05f;
                transform.position = sinkPos;
                yield return null;
            }

            gameObject.SetActive(false);
            transform.localScale = startScale;
            activeAnim = null;
        }

        private IEnumerator SmoothTransform(Vector3 targetPos, Quaternion targetRot, float duration)
        {
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = t * t * (3f - 2f * t);
                transform.SetPositionAndRotation(
                    Vector3.Lerp(startPos, targetPos, ease),
                    Quaternion.Slerp(startRot, targetRot, ease));
                yield return null;
            }

            transform.SetPositionAndRotation(targetPos, targetRot);
            activeAnim = null;
        }

        #endregion
    }

    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }
}
