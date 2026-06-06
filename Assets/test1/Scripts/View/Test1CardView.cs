using UnityEngine;
using UnityEngine.UI;

namespace Test1.BoardGame
{
    [RequireComponent(typeof(Button))]
    public sealed class Test1CardView : MonoBehaviour
    {
        public Button Button;
        public Image BackgroundImage;
        public Image SelectionFrame;
        public Text NumberText;
        public Text ColorText;
        public Text OwnerText;
        public Text SelectionText;

        private Test1BoardGameController controller;

        public string CardId { get; private set; }

        private void Reset()
        {
            Button = GetComponent<Button>();
            BackgroundImage = GetComponent<Image>();
        }

        public void Bind(
            Test1BoardGameController controller,
            Test1Card card,
            Test1CardVisibility visibility,
            bool selectable,
            bool selected)
        {
            this.controller = controller;
            CardId = card == null ? string.Empty : card.CardId;

            if (Button == null)
            {
                Button = GetComponent<Button>();
            }

            if (BackgroundImage == null)
            {
                BackgroundImage = GetComponent<Image>();
            }

            if (Button != null)
            {
                Button.onClick.RemoveListener(HandleClicked);
                Button.interactable = selectable;
                if (selectable)
                {
                    Button.onClick.AddListener(HandleClicked);
                }
            }

            ApplyText(card, visibility, selected);
            ApplyVisualState(card, visibility, selected);
        }

        private void ApplyText(Test1Card card, Test1CardVisibility visibility, bool selected)
        {
            bool showCard = card != null && visibility != Test1CardVisibility.Hidden;
            bool showNumber = card != null && visibility == Test1CardVisibility.FaceUp;

            SetText(NumberText, showNumber ? card.Number.ToString() : "?");
            SetText(ColorText, showCard ? card.Color.ToString() : "Hidden");
            SetText(OwnerText, showCard ? card.Owner.ToString() : string.Empty);
            SetText(SelectionText, selected ? "Selected" : string.Empty);
        }

        private void ApplyVisualState(Test1Card card, Test1CardVisibility visibility, bool selected)
        {
            if (BackgroundImage != null)
            {
                BackgroundImage.color = card == null || visibility == Test1CardVisibility.Hidden
                    ? Color.gray
                    : ToUnityColor(card.Color, visibility == Test1CardVisibility.FaceUp);
            }

            if (SelectionFrame != null)
            {
                SelectionFrame.gameObject.SetActive(selected);
            }
        }

        private void HandleClicked()
        {
            if (controller != null && !string.IsNullOrEmpty(CardId))
            {
                controller.SelectCardById(CardId);
            }
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static Color ToUnityColor(Test1CardColor color, bool faceUp)
        {
            float alpha = faceUp ? 1f : 0.75f;
            switch (color)
            {
                case Test1CardColor.Red:
                    return new Color(0.85f, 0.2f, 0.16f, alpha);
                case Test1CardColor.Green:
                    return new Color(0.16f, 0.62f, 0.25f, alpha);
                case Test1CardColor.Blue:
                    return new Color(0.18f, 0.38f, 0.85f, alpha);
                default:
                    return Color.white;
            }
        }
    }
}
