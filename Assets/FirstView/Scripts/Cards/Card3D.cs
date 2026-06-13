using UnityEngine;
using System.Collections;

namespace FirstView
{
    public class Card3D : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private MeshRenderer cardMeshRenderer;
        [SerializeField] private GameObject frontRoot;
        [SerializeField] private GameObject backRoot;
        [SerializeField] private UnityEngine.UI.Image frontImage;
        [SerializeField] private UnityEngine.UI.Image backImage;

        [Header("Card Data")]
        public string cardId;
        public string cardName = "Card";
        public int attack;
        public int health;
        public string ability = "";
        public int cost;
        public CardRarity rarity = CardRarity.Common;
        public Sprite frontSprite;
        public Sprite backSprite;

        [Header("Animation")]
        [SerializeField] private float hoverLiftHeight = 0.003f;
        [SerializeField] private float hoverTiltAngle = 5f;
        [SerializeField] private float animSmoothTime = 0.12f;
        [SerializeField] private float globalScale = 0.1f;

        public CardFacing facing = CardFacing.FacePlayer;
        public Transform faceTarget;

        private Vector3 basePosition;
        private Quaternion baseRotation;
        private bool isHovered;
        private Coroutine activeAnim;
        private Transform cachedSlotTransform;

        public bool IsHovered => isHovered;
        public Vector3 BasePosition => basePosition;

        private void Awake()
        {
            if (cardMeshRenderer == null) cardMeshRenderer = GetComponentInChildren<MeshRenderer>();
            transform.localScale = Vector3.one * globalScale;
            ResolveFrontBack();
            ApplySprites();
        }

        private void LateUpdate()
        {
            if (activeAnim != null) return;

            transform.position = isHovered
                ? basePosition + Vector3.up * hoverLiftHeight
                : basePosition;

            transform.rotation = ComputeFacingRotation();
        }

        private Quaternion ComputeFacingRotation()
        {
            switch (facing)
            {
                case CardFacing.FacePlayer:
                case CardFacing.FaceEnemy:
                {
                    Transform target = faceTarget;
                    if (target == null) return Quaternion.identity;
                    Vector3 toTarget = basePosition - target.position;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.0001f)
                        return Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                    return Quaternion.identity;
                }

                case CardFacing.FaceUp:
                    return Quaternion.Euler(90f, 0f, 0f);

                default:
                    return Quaternion.identity;
            }
        }

        public void SetSlotTransform(Transform slot)
        {
            cachedSlotTransform = slot;
        }

        private void ResolveFrontBack()
        {
            if (frontRoot != null && backRoot != null) return;

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var canvas = child.GetComponent<Canvas>();
                if (canvas == null) continue;

                if (frontRoot == null && child.localPosition.z < 0f)
                    frontRoot = child.gameObject;
                else if (backRoot == null && child.localPosition.z > 0f)
                    backRoot = child.gameObject;
            }

            if (frontRoot != null && frontImage == null)
                frontImage = frontRoot.GetComponentInChildren<UnityEngine.UI.Image>();
            if (backRoot != null && backImage == null)
                backImage = backRoot.GetComponentInChildren<UnityEngine.UI.Image>();
        }

        private void ApplySprites()
        {
            if (frontImage != null && frontSprite != null)
                frontImage.sprite = frontSprite;
            if (backImage != null && backSprite != null)
                backImage.sprite = backSprite;
        }

        public void SetFrontSprite(Sprite sprite)
        {
            frontSprite = sprite;
            if (frontImage != null) frontImage.sprite = sprite;
        }

        public void SetBackSprite(Sprite sprite)
        {
            backSprite = sprite;
            if (backImage != null) backImage.sprite = sprite;
        }

        public void ShowFront(bool show)
        {
            if (frontRoot != null) frontRoot.SetActive(true);
            if (backRoot != null) backRoot.SetActive(true);
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

        public void SetEmissionGlow(bool on, Color? color = null)
        {
            if (cardMeshRenderer == null) return;
            foreach (var mat in cardMeshRenderer.materials)
            {
                if (on)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color ?? new Color(0.3f, 0.25f, 0.1f));
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
            Quaternion restRot = ComputeFacingRotation();
            Quaternion targetRot = lift
                ? Quaternion.Euler(restRot.eulerAngles.x - hoverTiltAngle, restRot.eulerAngles.y, 0f)
                : restRot;

            yield return SmoothTransform(targetPos, targetRot, animSmoothTime);
        }

        private IEnumerator DrawSequence(Vector3 fromPos, Quaternion fromRot, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            Quaternion restRot = ComputeFacingRotation();
            transform.SetPositionAndRotation(fromPos, fromRot);
            ShowFront(false);

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
                transform.rotation = restRot * Quaternion.Euler(0f, flip, 0f);

                if (ease > 0.5f) ShowFront(true);

                yield return null;
            }

            transform.SetPositionAndRotation(basePosition, restRot);
            ShowFront(true);
            activeAnim = null;
        }

        private IEnumerator PlaceSequence(Vector3 targetPos, Quaternion targetRot)
        {
            basePosition = targetPos;
            baseRotation = targetRot;

            Quaternion restRot = ComputeFacingRotation();
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
                transform.SetPositionAndRotation(pos, Quaternion.Slerp(startRot, restRot, ease));
                yield return null;
            }

            transform.SetPositionAndRotation(targetPos, restRot);
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

    public enum CardFacing
    {
        FacePlayer,
        FaceUp,
        FaceEnemy
    }

    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }
}
