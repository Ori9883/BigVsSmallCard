using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Test1.BoardGame
{
    public sealed class Test1BoardGameView : MonoBehaviour
    {
        [Header("Controller")]
        public Test1BoardGameController Controller;

        [Header("Card Prefab And Roots")]
        public Test1CardView CardViewPrefab;
        public Transform PlayerAHandRoot;
        public Transform PlayerBHandRoot;
        public Transform PlayerAPlaySlot;
        public Transform PlayerBPlaySlot;
        public Transform RemovedCardSlot;

        [Header("Texts")]
        public Text RoundText;
        public Text PhaseText;
        public Text ScoreText;
        public Text PromptText;
        public Text RemovedCardText;
        public Text RoundResultText;
        public Text HistoryText;
        public Text FinalResultText;

        [Header("Buttons")]
        public Button StartButton;
        public Button ConfirmButton;
        public Button ContinueButton;
        public Button KeepRemovedButton;
        public Button SwapModeButton;
        public Button ConfirmSwapButton;

        [Header("Hot Seat Privacy")]
        public bool UsePassDevicePanel = true;
        public GameObject PassDevicePanel;
        public Text PassDeviceText;
        public Button PassDeviceButton;

        [Header("Debug")]
        public bool ShowAllNumbersForDebug;
        public bool RevealRemovedCardOnGameOver = true;

        private Test1PlayerId visiblePrivatePlayer = Test1PlayerId.None;
        private bool waitingForPassConfirmation;

        private void Awake()
        {
            if (Controller == null)
            {
                Controller = GetComponent<Test1BoardGameController>();
            }

            WireButtons();
        }

        private void OnEnable()
        {
            if (Controller != null)
            {
                Controller.OnStateChanged.AddListener(HandleStateChanged);
                Controller.OnSelectionChanged.AddListener(HandleSelectionChanged);
                HandleStateChanged(Controller.State);
            }
        }

        private void OnDisable()
        {
            if (Controller != null)
            {
                Controller.OnStateChanged.RemoveListener(HandleStateChanged);
                Controller.OnSelectionChanged.RemoveListener(HandleSelectionChanged);
            }
        }

        public void ConfirmPassDevice()
        {
            if (Controller == null || Controller.State == null)
            {
                return;
            }

            visiblePrivatePlayer = Controller.State.CurrentActor;
            waitingForPassConfirmation = false;
            SetActive(PassDevicePanel, false);
            Refresh(Controller.State);
        }

        private void WireButtons()
        {
            AddButtonListener(StartButton, () => { if (Controller != null) Controller.StartNewGame(); });
            AddButtonListener(ConfirmButton, () => { if (Controller != null) Controller.ConfirmSelectedCard(); });
            AddButtonListener(ContinueButton, () => { if (Controller != null) Controller.ContinueAfterRound(); });
            AddButtonListener(KeepRemovedButton, () => { if (Controller != null) Controller.KeepRemovedCard(); });
            AddButtonListener(SwapModeButton, () => { if (Controller != null) Controller.EnterSwapSelection(); });
            AddButtonListener(ConfirmSwapButton, () => { if (Controller != null) Controller.ConfirmSwapSelectedCard(); });
            AddButtonListener(PassDeviceButton, ConfirmPassDevice);
        }

        private void HandleStateChanged(Test1GameState state)
        {
            UpdatePassGate(state);
            Refresh(state);
        }

        private void HandleSelectionChanged(string cardId)
        {
            if (Controller != null)
            {
                Refresh(Controller.State);
            }
        }

        private void UpdatePassGate(Test1GameState state)
        {
            // Hot-seat privacy prevents one player from seeing the next player's hand numbers.
            if (state == null || !UsePassDevicePanel || !IsPrivatePhase(state.Phase) || state.CurrentActor == Test1PlayerId.None)
            {
                waitingForPassConfirmation = false;
                visiblePrivatePlayer = Test1PlayerId.None;
                SetActive(PassDevicePanel, false);
                return;
            }

            if (visiblePrivatePlayer != state.CurrentActor)
            {
                waitingForPassConfirmation = true;
            }

            SetActive(PassDevicePanel, waitingForPassConfirmation);
            SetText(PassDeviceText, "Pass device to " + PlayerName(state.CurrentActor) + ", then press Ready.");
        }

        private void Refresh(Test1GameState state)
        {
            if (state == null)
            {
                return;
            }

            BuildHand(PlayerAHandRoot, state.PlayerA, state);
            BuildHand(PlayerBHandRoot, state.PlayerB, state);
            BuildPlayedSlot(PlayerAPlaySlot, state.CurrentRound == null ? null : state.CurrentRound.GetPlayedCard(Test1PlayerId.PlayerA), state);
            BuildPlayedSlot(PlayerBPlaySlot, state.CurrentRound == null ? null : state.CurrentRound.GetPlayedCard(Test1PlayerId.PlayerB), state);
            BuildPlayedSlot(RemovedCardSlot, state.RemovedCard, state);
            RefreshTexts(state);
            RefreshButtons(state);
        }

        private void BuildHand(Transform root, Test1PlayerState player, Test1GameState state)
        {
            ClearRoot(root);
            if (root == null || player == null || CardViewPrefab == null)
            {
                return;
            }

            foreach (Test1Card card in player.HandCards)
            {
                CreateCardView(root, card, state);
            }
        }

        private void BuildPlayedSlot(Transform root, Test1Card card, Test1GameState state)
        {
            ClearRoot(root);
            if (root == null || card == null || CardViewPrefab == null)
            {
                return;
            }

            CreateCardView(root, card, state);
        }

        private void CreateCardView(Transform root, Test1Card card, Test1GameState state)
        {
            Test1CardView cardView = Instantiate(CardViewPrefab, root);
            bool selected = Controller != null && Controller.SelectedCardId == card.CardId;
            bool selectable = IsCardSelectable(card, state);
            cardView.Bind(Controller, card, GetVisibility(card, state), selectable, selected);
        }

        private Test1CardVisibility GetVisibility(Test1Card card, Test1GameState state)
        {
            // Hidden cards still show color because color is public by rule.
            if (card == null)
            {
                return Test1CardVisibility.Hidden;
            }

            if (ShowAllNumbersForDebug || card.IsFaceUp)
            {
                return Test1CardVisibility.FaceUp;
            }

            if (card.Zone == Test1CardZone.Removed)
            {
                bool peekOwnerCanSee = IsPrivateViewReadyFor(state.PeekOwner)
                    && (state.Phase == Test1GamePhase.PeekDecision || state.Phase == Test1GamePhase.WaitingForSwapCard);
                bool gameOverReveal = state.Phase == Test1GamePhase.GameOver && RevealRemovedCardOnGameOver;
                return peekOwnerCanSee || gameOverReveal ? Test1CardVisibility.FaceUp : Test1CardVisibility.FaceDownColorOnly;
            }

            if (card.Zone == Test1CardZone.Hand && IsPrivateViewReadyFor(card.Owner))
            {
                return Test1CardVisibility.FaceUp;
            }

            return Test1CardVisibility.FaceDownColorOnly;
        }

        private bool IsCardSelectable(Test1Card card, Test1GameState state)
        {
            if (Controller == null || card == null || waitingForPassConfirmation)
            {
                return false;
            }

            if (!IsSelectionPhase(state.Phase) || card.Zone != Test1CardZone.Hand)
            {
                return false;
            }

            return card.Owner == state.CurrentActor && IsPrivateViewReadyFor(state.CurrentActor);
        }

        private bool IsPrivateViewReadyFor(Test1PlayerId playerId)
        {
            if (playerId == Test1PlayerId.None)
            {
                return false;
            }

            return !UsePassDevicePanel || (!waitingForPassConfirmation && visiblePrivatePlayer == playerId);
        }

        private void RefreshTexts(Test1GameState state)
        {
            SetText(RoundText, state.Phase == Test1GamePhase.NotStarted ? "Round -" : "Round " + state.CurrentRoundIndex + " / 7");
            SetText(PhaseText, state.Phase.ToString());
            SetText(ScoreText, "Player A " + state.PlayerA.Score + "  :  " + state.PlayerB.Score + " Player B");
            SetText(PromptText, BuildPrompt(state));
            SetText(RemovedCardText, BuildRemovedCardText(state));
            SetText(RoundResultText, BuildRoundResultText(state));
            SetText(HistoryText, BuildHistoryText(state));
            SetText(FinalResultText, BuildFinalResultText(state));
        }

        private void RefreshButtons(Test1GameState state)
        {
            bool blocked = waitingForPassConfirmation;
            SetButton(StartButton, state.Phase == Test1GamePhase.NotStarted || state.Phase == Test1GamePhase.GameOver, true);
            SetButton(ConfirmButton, IsPlaySelectionPhase(state.Phase), !blocked && Controller != null && Controller.CanConfirmSelectedCard());
            SetButton(ContinueButton, state.Phase == Test1GamePhase.RoundSettlement, !blocked);
            SetButton(KeepRemovedButton, state.Phase == Test1GamePhase.PeekDecision, !blocked);
            SetButton(SwapModeButton, state.Phase == Test1GamePhase.PeekDecision, !blocked);
            SetButton(ConfirmSwapButton, state.Phase == Test1GamePhase.WaitingForSwapCard, !blocked && Controller != null && Controller.CanConfirmSelectedCard());
        }

        private string BuildPrompt(Test1GameState state)
        {
            if (waitingForPassConfirmation)
            {
                return "Waiting for " + PlayerName(state.CurrentActor) + " to confirm private view.";
            }

            switch (state.Phase)
            {
                case Test1GamePhase.NotStarted:
                    return "Press Start to begin a 7-round match.";
                case Test1GamePhase.WaitingForFirstPlayer:
                    return PlayerName(state.CurrentActor) + " selects the first face-down card.";
                case Test1GamePhase.WaitingForSecondPlayer:
                    return PlayerName(state.CurrentActor) + " selects the response card.";
                case Test1GamePhase.RoundSettlement:
                    return "Round settled. Press Continue.";
                case Test1GamePhase.PeekDecision:
                    return PlayerName(state.PeekOwner) + " may view the removed card and decide whether to swap.";
                case Test1GamePhase.WaitingForSwapCard:
                    return PlayerName(state.PeekOwner) + " selects one hand card to swap with the removed card.";
                case Test1GamePhase.GameOver:
                    return "Game over. Press Start to play again.";
                default:
                    return state.Phase.ToString();
            }
        }

        private string BuildRemovedCardText(Test1GameState state)
        {
            if (state.RemovedCard == null)
            {
                return "Removed card: none";
            }

            bool showNumber = GetVisibility(state.RemovedCard, state) == Test1CardVisibility.FaceUp;
            return "Removed card: " + state.RemovedCard.GetPublicName(showNumber);
        }

        private string BuildRoundResultText(Test1GameState state)
        {
            Test1RoundState round = state.CurrentRound;
            if (round == null || state.Phase != Test1GamePhase.RoundSettlement)
            {
                return string.Empty;
            }

            return "Round " + round.RoundIndex + ": " + FormatCard(round.GetPlayedCard(Test1PlayerId.PlayerA))
                + " vs " + FormatCard(round.GetPlayedCard(Test1PlayerId.PlayerB))
                + " => " + (round.Winner == Test1PlayerId.None ? "Tie" : PlayerName(round.Winner) + " +" + round.RoundScore);
        }

        private string BuildHistoryText(Test1GameState state)
        {
            if (state.RoundHistory == null || state.RoundHistory.Count == 0)
            {
                return "No completed rounds.";
            }

            StringBuilder builder = new StringBuilder();
            foreach (Test1RoundState round in state.RoundHistory)
            {
                builder.Append("R").Append(round.RoundIndex).Append(" ")
                    .Append(FormatCard(round.GetPlayedCard(Test1PlayerId.PlayerA))).Append(" / ")
                    .Append(FormatCard(round.GetPlayedCard(Test1PlayerId.PlayerB))).Append(" -> ")
                    .Append(round.Winner == Test1PlayerId.None ? "Tie" : PlayerName(round.Winner))
                    .Append("\n");
            }

            return builder.ToString();
        }

        private string BuildFinalResultText(Test1GameState state)
        {
            if (state.Phase != Test1GamePhase.GameOver)
            {
                return string.Empty;
            }

            if (state.FinalWinner == Test1PlayerId.None)
            {
                return "Final result: Tie. Score " + state.PlayerA.Score + " : " + state.PlayerB.Score;
            }

            return "Final result: " + PlayerName(state.FinalWinner) + " wins. Score " + state.PlayerA.Score + " : " + state.PlayerB.Score;
        }

        private static string FormatCard(Test1Card card)
        {
            return card == null ? "-" : card.Color + " " + card.Number;
        }

        private static bool IsSelectionPhase(Test1GamePhase phase)
        {
            return IsPlaySelectionPhase(phase) || phase == Test1GamePhase.WaitingForSwapCard;
        }

        private static bool IsPlaySelectionPhase(Test1GamePhase phase)
        {
            return phase == Test1GamePhase.WaitingForFirstPlayer || phase == Test1GamePhase.WaitingForSecondPlayer;
        }

        private static bool IsPrivatePhase(Test1GamePhase phase)
        {
            return IsSelectionPhase(phase) || phase == Test1GamePhase.PeekDecision;
        }

        private static string PlayerName(Test1PlayerId playerId)
        {
            switch (playerId)
            {
                case Test1PlayerId.PlayerA:
                    return "Player A";
                case Test1PlayerId.PlayerB:
                    return "Player B";
                default:
                    return "No player";
            }
        }

        private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void SetButton(Button button, bool active, bool interactable)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(active);
            button.interactable = interactable;
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static void ClearRoot(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (int index = root.childCount - 1; index >= 0; index--)
            {
                Transform child = root.GetChild(index);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
