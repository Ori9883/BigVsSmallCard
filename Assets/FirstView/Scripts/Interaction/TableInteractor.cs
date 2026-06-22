using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FirstView
{
    public class TableInteractor : MonoBehaviour
    {
        private static bool GetMouseButtonDown(int button)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                if (button == 0) return Mouse.current.leftButton.wasPressedThisFrame;
                if (button == 1) return Mouse.current.rightButton.wasPressedThisFrame;
            }
            return false;
#endif
        }

        private static bool GetKeyDown(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (key == KeyCode.Escape) return Keyboard.current.escapeKey.wasPressedThisFrame;
            }
            return false;
#endif
        }

        private static Vector3 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
#else
            return Input.mousePosition;
#endif
        }

        [Header("References")]
        [SerializeField] private FocusCameraRig cameraRig;
        [SerializeField] private Camera cam;

        [Header("Detection")]
        [SerializeField] private float raycastDistance = 4f;

        [Header("Cursor")]
        [SerializeField] private Texture2D defaultCursor;
        [SerializeField] private Texture2D hoverCursor;
        [SerializeField] private Vector2 cursorHotspot = new Vector2(6, 0);

        private Card3D hoveredCard;
        private CardSlot hoveredSlot;
        private DiscardPile hoveredDiscardPile;
        private readonly RaycastHit[] hits = new RaycastHit[8];
        private int hoverCount;

        private Card3D selectedCard;
        private bool HasSelection => selectedCard != null;

        public Card3D SelectedCard => selectedCard;
        public Card3D HoveredCard => hoveredCard;
        public CardSlot HoveredSlot => hoveredSlot;

        public System.Action<Card3D> OnCardClicked;
        public System.Action<Card3D, CardSlot> OnCardPlaced;
        public System.Action<Card3D> OnCardDeselected;
        public System.Action<string> OnEnvironmentClicked;
        public System.Action<DiscardPile> OnDiscardPileClicked;
        public System.Action OnPlayerHandReturnRequested;

        private void Awake()
        {
            if (cam == null) cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            DetectHover();
            UpdateCursor();

            if (GetMouseButtonDown(0))
                HandleClick();

            if (GetMouseButtonDown(1) || GetKeyDown(KeyCode.Escape))
            {
                if (HasSelection)
                    Deselect();
            }

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.wasPressedThisFrame)
                    cameraRig.FocusNext();
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                    OnPlayerHandReturnRequested?.Invoke();
                if (Keyboard.current.sKey.wasPressedThisFrame)
                {
                    if (cameraRig != null && cameraRig.CurrentFocusId == "DiscardPile")
                        OnPlayerHandReturnRequested?.Invoke();
                    else
                        cameraRig.FocusPrev();
                }
            }
#endif
        }

        private void DetectHover()
        {
            ClearHover();

            Vector3 mousePos = GetMousePosition();
            Ray ray = cam.ScreenPointToRay(mousePos);
            hoverCount = Physics.RaycastNonAlloc(ray, hits, raycastDistance);

            System.Array.Sort(hits, 0, hoverCount, new RaycastHitDistanceComparer());

            for (int i = 0; i < hoverCount; i++)
            {
                var card = FindCardInHierarchy(hits[i].collider);
                if (card != null && card.isActiveAndEnabled && card != selectedCard)
                {
                    hoveredCard = card;
                    if (card.owningPile != null)
                        hoveredDiscardPile = card.owningPile;
                    card.SetHover(true);
                    card.SetEmissionGlow(true, new Color(0.4f, 0.35f, 0.15f));
                    return;
                }

                var slot = FindSlotInHierarchy(hits[i].collider);
                if (slot != null && !slot.isOccupied)
                {
                    hoveredSlot = slot;
                    slot.SetHighlight(true);
                    return;
                }

                var pile = FindDiscardPileInHierarchy(hits[i].collider);
                if (pile != null)
                {
                    hoveredDiscardPile = pile;
                    return;
                }

                var zone = hits[i].collider.GetComponent<FocusZone>();
                if (zone != null)
                    return;
            }
        }

        private void ClearHover()
        {
            if (hoveredCard != null)
            {
                hoveredCard.SetHover(false);
                hoveredCard.SetEmissionGlow(false);
                hoveredCard = null;
            }
            if (hoveredSlot != null)
            {
                hoveredSlot.SetHighlight(false);
                hoveredSlot = null;
            }
            hoveredDiscardPile = null;
        }

        private void UpdateCursor()
        {
            bool showHover = hoveredCard != null || hoveredSlot != null || hoveredDiscardPile != null;
            if (showHover && hoverCursor != null)
                Cursor.SetCursor(hoverCursor, cursorHotspot, CursorMode.Auto);
            else if (!showHover && defaultCursor != null)
                Cursor.SetCursor(defaultCursor, cursorHotspot, CursorMode.Auto);
            else
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        private void HandleClick()
        {
            // Priority 1: if we have a selected card and click a valid slot → place it
            if (HasSelection && hoveredSlot != null && hoveredSlot.owner == SlotOwner.Player)
            {
                OnCardPlaced?.Invoke(selectedCard, hoveredSlot);
                Deselect();
                return;
            }

            // Priority 2: click a hand card → select it; click discard pile card → toggle expand
            if (hoveredCard != null)
            {
                if (hoveredDiscardPile != null)
                    OnDiscardPileClicked?.Invoke(hoveredDiscardPile);
                else
                    OnCardClicked?.Invoke(hoveredCard);
                return;
            }

            // Priority 2.5: click discard pile area (empty pile collider)
            if (hoveredDiscardPile != null)
            {
                OnDiscardPileClicked?.Invoke(hoveredDiscardPile);
                return;
            }

            // Priority 3: click empty space → deselect if selected, else check env
            if (HasSelection)
            {
                Deselect();
                return;
            }

            // Priority 4: environment click
            Vector3 mousePos = GetMousePosition();
            Ray ray = cam.ScreenPointToRay(mousePos);
            int count = Physics.RaycastNonAlloc(ray, hits, raycastDistance);
            for (int i = 0; i < count; i++)
            {
                var zone = hits[i].collider.GetComponent<FocusZone>();
                if (zone != null)
                {
                    cameraRig.FocusTo(zone.focusTargetId);
                    OnEnvironmentClicked?.Invoke(zone.focusTargetId);
                    return;
                }
            }

            OnEnvironmentClicked?.Invoke(string.Empty);
        }

        public void SelectCard(Card3D card)
        {
            if (selectedCard == card) return;
            Deselect();
            selectedCard = card;
            selectedCard.SetHover(true);
            selectedCard.SetEmissionGlow(true, new Color(0.6f, 0.5f, 0.15f));
        }

        public void Deselect()
        {
            if (selectedCard == null) return;
            selectedCard.SetHover(false);
            selectedCard.SetEmissionGlow(false);
            selectedCard = null;
            OnCardDeselected?.Invoke(selectedCard);
        }

        private static Card3D FindCardInHierarchy(Collider col)
        {
            var card = col.GetComponent<Card3D>();
            if (card != null) return card;
            var parent = col.transform.parent;
            if (parent != null) return parent.GetComponent<Card3D>();
            return null;
        }

        private static CardSlot FindSlotInHierarchy(Collider col)
        {
            var slot = col.GetComponent<CardSlot>();
            if (slot != null) return slot;
            var parent = col.transform.parent;
            if (parent != null) return parent.GetComponent<CardSlot>();
            return null;
        }

        private static DiscardPile FindDiscardPileInHierarchy(Collider col)
        {
            var pile = col.GetComponent<DiscardPile>();
            if (pile != null) return pile;
            var parent = col.transform.parent;
            if (parent != null) return parent.GetComponent<DiscardPile>();
            return null;
        }

        private struct RaycastHitDistanceComparer : System.Collections.IComparer
        {
            public int Compare(object x, object y)
            {
                return ((RaycastHit)x).distance.CompareTo(((RaycastHit)y).distance);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (cam == null) return;
            Vector3 mousePos = GetMousePosition();
            Ray ray = cam.ScreenPointToRay(mousePos);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(ray.origin, ray.direction * raycastDistance);
        }
#endif
    }

    public class FocusZone : MonoBehaviour
    {
        public string focusTargetId;
    }
}
