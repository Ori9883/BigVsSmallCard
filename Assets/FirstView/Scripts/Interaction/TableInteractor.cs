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
        private readonly RaycastHit[] hits = new RaycastHit[8];
        private int hoverCount;

        public Card3D HoveredCard => hoveredCard;
        public CardSlot HoveredSlot => hoveredSlot;

        public System.Action<Card3D> OnCardClicked;
        public System.Action<CardSlot> OnSlotClicked;
        public System.Action<string> OnEnvironmentClicked;

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
                cameraRig.FocusTo("Idle");
        }

        private void DetectHover()
        {
            ClearHover();

            Vector3 mousePos = GetMousePosition();
            Ray ray = cam.ScreenPointToRay(mousePos);
            hoverCount = Physics.RaycastNonAlloc(ray, hits, raycastDistance);

            // Sort by distance (closest first)
            System.Array.Sort(hits, 0, hoverCount, new RaycastHitDistanceComparer());

            for (int i = 0; i < hoverCount; i++)
            {
                var card = hits[i].collider.GetComponent<Card3D>();
                if (card != null && card.isActiveAndEnabled)
                {
                    hoveredCard = card;
                    card.SetHover(true);
                    card.SetEmissionGlow(true, new Color(0.4f, 0.35f, 0.15f));
                    return;
                }

                var slot = hits[i].collider.GetComponent<CardSlot>();
                if (slot != null && !slot.isOccupied)
                {
                    hoveredSlot = slot;
                    slot.SetHighlight(true);
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
        }

        private void UpdateCursor()
        {
            if (hoveredCard != null || hoveredSlot != null)
            {
                if (hoverCursor != null)
                    Cursor.SetCursor(hoverCursor, cursorHotspot, CursorMode.Auto);
                else
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
            else
            {
                if (defaultCursor != null)
                    Cursor.SetCursor(defaultCursor, cursorHotspot, CursorMode.Auto);
                else
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }

        private void HandleClick()
        {
            if (hoveredCard != null)
            {
                OnCardClicked?.Invoke(hoveredCard);
                return;
            }

            if (hoveredSlot != null)
            {
                OnSlotClicked?.Invoke(hoveredSlot);
                return;
            }

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
