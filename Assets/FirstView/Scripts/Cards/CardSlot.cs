using UnityEngine;

namespace FirstView
{
    /// <summary>
    /// Represents a slot on the table where cards can be placed.
    /// Provides visual feedback for valid placement targets.
    /// </summary>
    public class CardSlot : MonoBehaviour
    {
        [Header("Slot Config")]
        public string slotId;
        public SlotOwner owner = SlotOwner.Player;
        public int slotIndex;
        public bool isOccupied;

        [Header("Visuals")]
        [SerializeField] private MeshRenderer slotRenderer;
        [SerializeField] private Color emptyColor = new Color(0.8f, 0.7f, 0.3f, 0.15f);
        [SerializeField] private Color hoverColor = new Color(0.9f, 0.8f, 0.2f, 0.4f);
        [SerializeField] private Color occupiedColor = new Color(0.3f, 0.3f, 0.3f, 0.1f);

        private Material slotMaterial;

        public Card3D CurrentCard { get; set; }

        private void Awake()
        {
            if (slotRenderer == null) slotRenderer = GetComponentInChildren<MeshRenderer>();
        }

        public void SetHighlight(bool active)
        {
            if (slotMaterial == null && slotRenderer != null)
                slotMaterial = slotRenderer.material;

            if (slotMaterial == null) return;

            if (isOccupied)
                slotMaterial.color = occupiedColor;
            else if (active)
                slotMaterial.color = hoverColor;
            else
                slotMaterial.color = emptyColor;
        }

        public void PlaceCard(Card3D card)
        {
            CurrentCard = card;
            isOccupied = true;
            card.PlayPlaceAnimation(transform.position, transform.rotation);
            SetHighlight(false);
        }

        public void RemoveCard()
        {
            CurrentCard = null;
            isOccupied = false;
            SetHighlight(false);
        }

        public Vector3 GetCardPosition() => transform.position;
        public Quaternion GetCardRotation() => transform.rotation;
    }

    public enum SlotOwner
    {
        Player,
        Opponent
    }
}
