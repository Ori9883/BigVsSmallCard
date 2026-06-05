using System;
using UnityEngine;
using UnityEngine.UI;

namespace BoardGame
{
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(Button))]
    public sealed class CardView : MonoBehaviour
    {
        private Image background;
        private Button button;
        private Outline outline;
        private Text label;
        private CardData card;
        private Action<CardData> clickHandler;

        public string CardId
        {
            get { return card != null ? card.CardId : string.Empty; }
        }

        public void Bind(CardData cardData, bool showNumber, bool interactable, bool selected, Action<CardData> onClick)
        {
            EnsureReferences();

            card = cardData;
            clickHandler = onClick;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
            SetInteractable(interactable);
            SetSelected(selected);

            background.color = GetCardColor(cardData.Color, showNumber, interactable);
            label.text = BuildLabel(cardData, showNumber);
            label.color = showNumber ? Color.white : new Color(0.94f, 0.94f, 0.88f, 1f);
        }

        public void SetSelected(bool selected)
        {
            EnsureReferences();
            outline.enabled = selected;
            outline.effectColor = new Color(1f, 0.84f, 0.25f, 1f);
            outline.effectDistance = new Vector2(5f, -5f);
            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.localScale = selected ? Vector3.one * 1.07f : Vector3.one;
            }
        }

        public void SetInteractable(bool interactable)
        {
            EnsureReferences();
            button.interactable = interactable;
        }

        private void HandleClick()
        {
            if (card != null && clickHandler != null)
            {
                clickHandler(card);
            }
        }

        private void EnsureReferences()
        {
            if (background == null)
            {
                background = GetComponent<Image>();
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (outline == null)
            {
                outline = GetComponent<Outline>();
                if (outline == null)
                {
                    outline = gameObject.AddComponent<Outline>();
                }
            }

            if (label == null)
            {
                label = GetComponentInChildren<Text>();
                if (label == null)
                {
                    GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                    textObject.transform.SetParent(transform, false);
                    RectTransform textRect = textObject.GetComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                    label = textObject.GetComponent<Text>();
                    label.alignment = TextAnchor.MiddleCenter;
                    label.fontSize = 30;
                    label.fontStyle = FontStyle.Bold;
                    label.raycastTarget = false;
                    label.font = BoardGameUiFactory.GetDefaultFont();
                }
            }
        }

        private static string BuildLabel(CardData cardData, bool showNumber)
        {
            string colorCode = cardData.Color == CardColor.Red ? "R" : cardData.Color == CardColor.Green ? "G" : "B";
            return showNumber ? colorCode + "\n" + cardData.Number : colorCode + "\n?";
        }

        private static Color GetCardColor(CardColor cardColor, bool showNumber, bool interactable)
        {
            Color color;
            switch (cardColor)
            {
                case CardColor.Red:
                    color = new Color(0.73f, 0.18f, 0.15f, 1f);
                    break;
                case CardColor.Green:
                    color = new Color(0.16f, 0.53f, 0.31f, 1f);
                    break;
                default:
                    color = new Color(0.12f, 0.32f, 0.68f, 1f);
                    break;
            }

            if (!showNumber)
            {
                color = Color.Lerp(color, new Color(0.05f, 0.08f, 0.1f, 1f), 0.28f);
            }

            if (!interactable)
            {
                color.a = 0.72f;
            }

            return color;
        }
    }
}
