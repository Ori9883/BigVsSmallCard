using System.Collections.Generic;
using UnityEngine;

namespace FirstView
{
    public class DiscardPile : MonoBehaviour
    {
        [SerializeField] private Transform pileAnchor;
        [SerializeField] private float stackOffset = 0.002f;
        [SerializeField] private float fanSpacing = 0.05f;
        [SerializeField] private float fanArcHeight = 0.02f;
        [SerializeField] private Vector3 clickColliderSize = new Vector3(0.12f, 0.05f, 0.16f);

        private readonly List<Card3D> cards = new List<Card3D>();
        private bool expanded;

        private void Awake()
        {
            if (pileAnchor == null) pileAnchor = transform;
            EnsureCollider();
        }

        private void EnsureCollider()
        {
            if (GetComponent<Collider>() != null) return;

            var box = gameObject.AddComponent<BoxCollider>();
            box.size = clickColliderSize;
            box.isTrigger = true;
            box.center = transform.InverseTransformPoint(pileAnchor.position);
        }

        public void AddCard(Card3D card)
        {
            card.owningPile = this;
            cards.Add(card);
            LayoutCards();
        }

        public void Clear()
        {
            foreach (var c in cards)
                if (c != null) c.owningPile = null;
            cards.Clear();
            expanded = false;
        }

        public void ToggleExpand()
        {
            expanded = !expanded;
            LayoutCards();
        }

        public bool IsExpanded => expanded;

        private void LayoutCards()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                Card3D c = cards[i];
                if (c == null) continue;

                Vector3 pos;
                Quaternion rot;

                if (expanded)
                {
                    int cols = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(cards.Count)));
                    int row = i / cols;
                    int col = i % cols;
                    pos = pileAnchor.position
                          + pileAnchor.right * col * fanSpacing
                          + pileAnchor.forward * row * fanSpacing
                          + pileAnchor.up * fanArcHeight;
                    rot = Quaternion.Euler(90f, 0f, 0f);
                }
                else
                {
                    pos = pileAnchor.position + Vector3.up * stackOffset * i;
                    rot = Quaternion.Euler(90f, 0f, 0f);
                }

                c.SetBasePose(pos, rot);
                c.PlayPlaceAnimation(pos, rot);
            }
        }
    }
}
