using System.Collections.Generic;
using UnityEngine;

namespace FirstView
{
    public class DiscardPile : MonoBehaviour
    {
        [SerializeField] private Transform pileAnchor;
        [SerializeField] private float stackOffset = 0.002f;
        [SerializeField] private float fanArcHeight = 0f;
        [SerializeField] private Vector3 emptyPileColliderSize = new Vector3(0.08f, 0.01f, 0.12f);

        private readonly List<Card3D> cards = new List<Card3D>();
        private bool expanded;

        private void Awake()
        {
            if (pileAnchor == null) pileAnchor = transform;
            EnsureCollider();
        }

        private void EnsureCollider()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null)
                box = gameObject.AddComponent<BoxCollider>();
            box.size = emptyPileColliderSize;
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

        public void DestroyCardsAndClear()
        {
            foreach (var c in cards)
            {
                if (c != null)
                {
                    c.owningPile = null;
                    Destroy(c.gameObject);
                }
            }

            cards.Clear();
            expanded = false;

            var pileCol = GetComponent<Collider>();
            if (pileCol != null)
                pileCol.enabled = true;
        }

        public void ToggleExpand()
        {
            expanded = !expanded;
            LayoutCards();
        }

        public bool IsExpanded => expanded;

        private void LayoutCards()
        {
            float colSpacing = 0.062f;
            float rowSpacing = 0.1f;
            if (cards.Count > 0 && cards[0] != null)
            {
                var cardCol = cards[0].GetComponent<BoxCollider>();
                if (cardCol != null)
                {
                    Vector3 lossy = cards[0].transform.lossyScale;
                    colSpacing = cardCol.size.x * lossy.x;
                    rowSpacing = cardCol.size.y * lossy.y;
                }
            }

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
                    int rowCount = Mathf.CeilToInt(cards.Count / (float)cols) - 1;
                    int colCount = cols - 1;

                    pos = pileAnchor.position
                          + pileAnchor.right * (col * colSpacing - colCount * colSpacing * 0.5f)
                          + pileAnchor.forward * (row * rowSpacing - rowCount * rowSpacing * 0.5f)
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

            var pileCol = GetComponent<Collider>();
            if (pileCol != null)
                pileCol.enabled = (cards.Count == 0);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Vector3 center = pileAnchor != null ? pileAnchor.position : transform.position;

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(center, 0.005f);

            var col = GetComponent<Collider>();
            if (col is BoxCollider box && col.enabled)
            {
                Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
                Gizmos.DrawCube(
                    transform.TransformPoint(box.center),
                    Vector3.Scale(box.size, transform.lossyScale));
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
                Gizmos.DrawWireCube(
                    transform.TransformPoint(box.center),
                    Vector3.Scale(box.size, transform.lossyScale));
            }

            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null) continue;
                var cb = cards[i].GetComponent<BoxCollider>();
                if (cb == null) continue;
                Vector3 wCenter = cards[i].transform.TransformPoint(cb.center);
                Vector3 wSize = Vector3.Scale(cb.size, cards[i].transform.lossyScale);
                Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.5f);
                Gizmos.DrawWireCube(wCenter, wSize);
            }
        }
#endif
    }
}
