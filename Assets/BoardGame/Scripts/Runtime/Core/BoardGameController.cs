using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BoardGame
{
    public sealed class BoardGameController : MonoBehaviour
    {
        [Header("Rules")]
        public GameRuleConfig RuleConfig;
        public int RandomSeed;
        public bool UseRandomSeed = true;
        public bool RevealAllHandsForPrototype;

        [Header("Runtime State")]
        public GameSessionState Session;

        private Transform opponentHandArea;
        private Transform playerHandArea;
        private Transform playArea;
        private Transform historyContent;
        private Transform scoreContent;
        private Text titleText;
        private Text promptText;
        private Text scoreText;
        private Text removedText;
        private Text phaseText;
        private Text roundResultText;
        private Button startButton;
        private Button confirmButton;
        private Button passButton;
        private Button noSwapButton;
        private Button swapModeButton;
        private Button confirmSwapButton;
        private GameObject settlementPanel;
        private Text settlementText;

        private readonly List<CardView> cardViewPool = new List<CardView>();
        private readonly List<Text> scoreRows = new List<Text>();
        private CardData selectedCard;
        private RoundState currentRound;
        private TurnManager turnManager;
        private System.Random runtimeRandom;
        private PlayerId activePlayer = PlayerId.None;
        private bool waitingForPass;
        private bool swapSelectionMode;

        private void Awake()
        {
            if (RuleConfig == null)
            {
                RuleConfig = GameRuleConfig.CreateRuntimeDefault();
            }

            BuildUiIfNeeded();
        }

        private void Start()
        {
            ShowMainMenu();
        }

        public void SetupPrototypeInEditor()
        {
            if (RuleConfig == null)
            {
                RuleConfig = GameRuleConfig.CreateRuntimeDefault();
            }

            BuildUiIfNeeded();
            ShowMainMenu();
        }

        public void StartNewGame()
        {
            selectedCard = null;
            currentRound = null;
            activePlayer = PlayerId.None;
            waitingForPass = false;
            swapSelectionMode = false;

            int seed = UseRandomSeed || RandomSeed == 0 ? Environment.TickCount : RandomSeed;
            RandomSeed = seed;
            runtimeRandom = new System.Random(seed);
            turnManager = new TurnManager(RuleConfig.TurnPolicy, runtimeRandom);

            DealResult dealResult = DealService.Deal(seed, RuleConfig);
            Session = new GameSessionState
            {
                RandomSeed = seed,
                Phase = GamePhase.Dealing,
                CurrentRoundIndex = 0,
                PlayerA = new PlayerState(PlayerId.PlayerA, "Player A"),
                PlayerB = new PlayerState(PlayerId.PlayerB, "Player B"),
                RemovedCard = dealResult.RemovedCard,
                AllCards = dealResult.AllCards,
                PeekOwner = PlayerId.None,
                HasPeekResolved = false,
                LastRoundFirstPlayer = PlayerId.None,
                LastRoundWinner = PlayerId.None
            };

            Session.PlayerA.HandCards = dealResult.PlayerAHand;
            Session.PlayerB.HandCards = dealResult.PlayerBHand;
            Session.AllCards.Add(Session.RemovedCard);

            LogMatchStart(dealResult);
            settlementPanel.SetActive(false);
            BeginNextRound();
        }

        private void ShowMainMenu()
        {
            Session = null;
            titleText.text = "Fifteen Cards - Big vs Small";
            promptText.text = "Local two-player hot-seat prototype. Colors are public; numbers are hidden until reveal.";
            phaseText.text = "Read the rule summary, then start a full 7-round match.";
            scoreText.text = "Round scores: 10 / 10 / 20 / 20 / 30 / 30 / 20";
            removedText.text = "Removed card: hidden until a peek owner is decided after round 3.";
            roundResultText.text = "Rule: higher number wins, but 1 beats 5. Same number is a tie.";
            startButton.gameObject.SetActive(true);
            confirmButton.gameObject.SetActive(false);
            passButton.gameObject.SetActive(false);
            noSwapButton.gameObject.SetActive(false);
            swapModeButton.gameObject.SetActive(false);
            confirmSwapButton.gameObject.SetActive(false);
            settlementPanel.SetActive(false);
            ClearChildren(opponentHandArea);
            ClearChildren(playerHandArea);
            ClearChildren(playArea);
            ClearChildren(historyContent);
            RefreshScorePanel(null);
        }

        private void BeginNextRound()
        {
            if (Session.CurrentRoundIndex >= RuleConfig.RoundScores.Length)
            {
                ShowGameSettlement();
                return;
            }

            Session.CurrentRoundIndex++;
            Session.Phase = GamePhase.RoundStart;
            selectedCard = null;
            swapSelectionMode = false;

            PlayerId first = turnManager.GetFirstPlayer(Session.CurrentRoundIndex, Session.LastRoundFirstPlayer, Session.LastRoundWinner);
            PlayerId second = TurnManager.OpponentOf(first);
            currentRound = new RoundState
            {
                RoundIndex = Session.CurrentRoundIndex,
                RoundScore = RuleConfig.RoundScores[Session.CurrentRoundIndex - 1],
                FirstPlayer = first,
                SecondPlayer = second,
                Winner = PlayerId.None
            };

            Session.LastRoundFirstPlayer = first;
            BeginSelection(first, GamePhase.FirstPlayerSelecting);
        }

        private void BeginSelection(PlayerId player, GamePhase phase)
        {
            activePlayer = player;
            selectedCard = null;
            waitingForPass = phase == GamePhase.SecondPlayerSelecting || (phase == GamePhase.FirstPlayerSelecting && Session.CurrentRoundIndex > 1);
            Session.Phase = phase;
            RefreshAll();
        }

        public void ConfirmSelection()
        {
            if (Session == null || selectedCard == null)
            {
                return;
            }

            if (swapSelectionMode)
            {
                ConfirmSwap();
                return;
            }

            PlayerState player = GetPlayer(activePlayer);
            if (!player.HandCards.Contains(selectedCard))
            {
                return;
            }

            player.HandCards.Remove(selectedCard);
            selectedCard.Owner = activePlayer;
            selectedCard.Zone = CardZone.Played;
            selectedCard.IsFaceUp = false;
            selectedCard.PlayedRoundIndex = currentRound.RoundIndex;

            if (Session.Phase == GamePhase.FirstPlayerSelecting)
            {
                currentRound.FirstPlayedCard = selectedCard;
                BeginSelection(currentRound.SecondPlayer, GamePhase.SecondPlayerSelecting);
            }
            else if (Session.Phase == GamePhase.SecondPlayerSelecting)
            {
                currentRound.SecondPlayedCard = selectedCard;
                RevealAndSettleRound();
            }
        }

        public void ContinueAfterPass()
        {
            if (Session != null && Session.Phase == GamePhase.RoundSettlement)
            {
                AdvanceAfterRoundSettlement();
                return;
            }

            waitingForPass = false;
            RefreshAll();
        }

        public void ChooseNoSwap()
        {
            if (Session == null || Session.Phase != GamePhase.PeekAndSwap)
            {
                return;
            }

            Session.HasPeekResolved = true;
            roundResultText.text = PlayerName(Session.PeekOwner) + " looked at the removed card and kept all cards.";
            BeginNextRound();
        }

        public void EnterSwapMode()
        {
            if (Session == null || Session.Phase != GamePhase.PeekAndSwap || Session.PeekOwner == PlayerId.None)
            {
                return;
            }

            activePlayer = Session.PeekOwner;
            selectedCard = null;
            swapSelectionMode = true;
            waitingForPass = false;
            RefreshAll();
        }

        private void ConfirmSwap()
        {
            if (Session == null || selectedCard == null || Session.PeekOwner == PlayerId.None)
            {
                return;
            }

            PlayerState player = GetPlayer(Session.PeekOwner);
            if (!player.HandCards.Contains(selectedCard))
            {
                return;
            }

            CardData oldRemovedCard = Session.RemovedCard;
            player.HandCards.Remove(selectedCard);
            selectedCard.Owner = PlayerId.None;
            selectedCard.Zone = CardZone.Removed;
            selectedCard.IsFaceUp = false;

            oldRemovedCard.Owner = player.PlayerId;
            oldRemovedCard.Zone = CardZone.Hand;
            oldRemovedCard.IsFaceUp = false;
            player.HandCards.Add(oldRemovedCard);
            DealService.SortHand(player.HandCards);

            Session.RemovedCard = selectedCard;
            Session.HasPeekResolved = true;
            Session.SwapOccurred = true;
            Session.SwapPlayer = player.PlayerId;
            Session.SwappedOutCardId = selectedCard.CardId;
            Session.SwappedInCardId = oldRemovedCard.CardId;
            swapSelectionMode = false;
            selectedCard = null;
            Debug.Log("[BoardGame] Swap: " + PlayerName(player.PlayerId) + " took " + oldRemovedCard.CardId + " and removed " + Session.RemovedCard.CardId);
            BeginNextRound();
        }

        private void RevealAndSettleRound()
        {
            Session.Phase = GamePhase.Reveal;
            currentRound.FirstPlayedCard.IsFaceUp = true;
            currentRound.SecondPlayedCard.IsFaceUp = true;

            PlayerId winner = RuleEngine.Compare(currentRound.FirstPlayedCard, currentRound.SecondPlayedCard);
            currentRound.Winner = winner;
            currentRound.ResultType = RuleEngine.GetResultType(winner);

            if (winner != PlayerId.None)
            {
                GetPlayer(winner).Score += currentRound.RoundScore;
                if (currentRound.RoundIndex <= 3)
                {
                    GetPlayer(winner).FirstThreeRoundWinCount++;
                }
            }

            currentRound.ScoreAfterRoundA = Session.PlayerA.Score;
            currentRound.ScoreAfterRoundB = Session.PlayerB.Score;
            Session.RoundHistory.Add(currentRound);
            Session.LastRoundWinner = winner;

            MovePlayedCardsToDiscard(currentRound);
            Session.Phase = GamePhase.RoundSettlement;
            RefreshAll();
        }

        private void AdvanceAfterRoundSettlement()
        {
            if (currentRound.RoundIndex == 3 && RuleConfig.EnablePeekSwap)
            {
                ResolvePeekAndMaybeSwap();
                return;
            }

            if (currentRound.RoundIndex >= RuleConfig.RoundScores.Length)
            {
                ShowGameSettlement();
                return;
            }

            BeginNextRound();
        }

        private void ResolvePeekAndMaybeSwap()
        {
            Session.PeekOwner = RuleEngine.ResolvePeekOwner(Session.RoundHistory.Take(3).ToList());
            Session.HasPeekResolved = Session.PeekOwner == PlayerId.None;

            if (Session.PeekOwner == PlayerId.None)
            {
                roundResultText.text += " No player gained peek rights after round 3.";
                BeginNextRound();
                return;
            }

            Session.Phase = GamePhase.PeekAndSwap;
            activePlayer = Session.PeekOwner;
            waitingForPass = true;
            selectedCard = null;
            RefreshAll();
        }

        private void ShowGameSettlement()
        {
            Session.Phase = GamePhase.GameSettlement;
            PlayerId winner = RuleEngine.ResolveGameWinner(Session.PlayerA, Session.PlayerB);
            string winnerText = winner == PlayerId.None ? "Tie Game" : PlayerName(winner) + " Wins";
            settlementText.text = winnerText + "\n\nFinal Score\nPlayer A: " + Session.PlayerA.Score + "\nPlayer B: " + Session.PlayerB.Score + "\n\nSeed: " + Session.RandomSeed + "\nPeek owner: " + PlayerName(Session.PeekOwner) + "\nSwap: " + (Session.SwapOccurred ? Session.SwappedInCardId + " <-> " + Session.SwappedOutCardId : "No swap") + "\n\nRound history is listed on the left panel.";
            settlementPanel.SetActive(true);
            RefreshAll();
            Debug.Log("[BoardGame] Game over: " + winnerText + ", score A=" + Session.PlayerA.Score + ", B=" + Session.PlayerB.Score + ", seed=" + Session.RandomSeed);
        }

        private void MovePlayedCardsToDiscard(RoundState round)
        {
            AddDiscard(round.FirstPlayedCard);
            AddDiscard(round.SecondPlayedCard);
        }

        private void AddDiscard(CardData card)
        {
            card.Zone = CardZone.Discard;
            card.IsFaceUp = true;
            GetPlayer(card.Owner).DiscardCards.Add(card);
        }

        private void OnCardClicked(CardData card)
        {
            if (waitingForPass || Session == null)
            {
                return;
            }

            if (Session.Phase != GamePhase.FirstPlayerSelecting && Session.Phase != GamePhase.SecondPlayerSelecting && !swapSelectionMode)
            {
                return;
            }

            if (card.Owner != activePlayer || card.Zone != CardZone.Hand)
            {
                return;
            }

            selectedCard = selectedCard == card ? null : card;
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (Session == null)
            {
                return;
            }

            startButton.gameObject.SetActive(false);
            bool canSelect = !waitingForPass && (Session.Phase == GamePhase.FirstPlayerSelecting || Session.Phase == GamePhase.SecondPlayerSelecting || swapSelectionMode);
            confirmButton.gameObject.SetActive(canSelect && !swapSelectionMode);
            confirmButton.interactable = selectedCard != null;
            passButton.gameObject.SetActive(waitingForPass && Session.Phase != GamePhase.GameSettlement);
            noSwapButton.gameObject.SetActive(Session.Phase == GamePhase.PeekAndSwap && !waitingForPass && !swapSelectionMode);
            swapModeButton.gameObject.SetActive(Session.Phase == GamePhase.PeekAndSwap && !waitingForPass && !swapSelectionMode);
            confirmSwapButton.gameObject.SetActive(swapSelectionMode);
            confirmSwapButton.interactable = selectedCard != null;
            passButton.gameObject.SetActive((waitingForPass || Session.Phase == GamePhase.RoundSettlement) && Session.Phase != GamePhase.GameSettlement);
            SetButtonText(passButton, Session.Phase == GamePhase.RoundSettlement ? "Continue" : "I Am Ready");

            RefreshTexts();
            RefreshHands();
            RefreshPlayArea();
            RefreshHistory();
            RefreshScorePanel(Session);
        }

        private void RefreshTexts()
        {
            titleText.text = "Fifteen Cards - Big vs Small";
            phaseText.text = "Phase: " + Session.Phase + " | Seed: " + Session.RandomSeed;
            scoreText.text = "Player A " + Session.PlayerA.Score + " : " + Session.PlayerB.Score + " Player B";

            bool showRemovedNumber = Session.Phase == GamePhase.PeekAndSwap && Session.PeekOwner == activePlayer && !waitingForPass;
            removedText.text = showRemovedNumber
                ? "Removed card: " + CardLabel(Session.RemovedCard, true)
                : "Removed card: " + ColorLabel(Session.RemovedCard.Color) + " ?";

            if (Session.Phase == GamePhase.PeekAndSwap)
            {
                promptText.text = waitingForPass
                    ? "Pass the device to " + PlayerName(Session.PeekOwner) + " for private peek/swap."
                    : swapSelectionMode
                        ? PlayerName(Session.PeekOwner) + ", choose one hand card to exchange with the removed card."
                        : PlayerName(Session.PeekOwner) + " may view the removed card and decide whether to swap.";
                roundResultText.text = "Peek card is private: " + (showRemovedNumber ? CardLabel(Session.RemovedCard, true) : ColorLabel(Session.RemovedCard.Color) + " ?");
                return;
            }

            if (Session.Phase == GamePhase.RoundSettlement)
            {
                promptText.text = "Round " + currentRound.RoundIndex + " complete. Review the revealed cards and click Continue.";
                RoundState last = Session.RoundHistory[Session.RoundHistory.Count - 1];
                roundResultText.text = "Last round: " + CardLabel(last.FirstPlayedCard, true) + " vs " + CardLabel(last.SecondPlayedCard, true) + " => " + (last.Winner == PlayerId.None ? "Tie" : PlayerName(last.Winner) + " +" + last.RoundScore);
                return;
            }

            if (Session.Phase == GamePhase.GameSettlement)
            {
                promptText.text = "Match complete.";
                return;
            }

            if (waitingForPass)
            {
                promptText.text = "Pass the device to " + PlayerName(activePlayer) + ". Hand numbers stay hidden until confirmed.";
            }
            else
            {
                promptText.text = PlayerName(activePlayer) + " selects one card. Current round: " + Session.CurrentRoundIndex + " / 7, score " + currentRound.RoundScore;
            }

            if (Session.RoundHistory.Count > 0)
            {
                RoundState last = Session.RoundHistory[Session.RoundHistory.Count - 1];
                roundResultText.text = "Last round: " + CardLabel(last.FirstPlayedCard, true) + " vs " + CardLabel(last.SecondPlayedCard, true) + " => " + (last.Winner == PlayerId.None ? "Tie" : PlayerName(last.Winner) + " +" + last.RoundScore);
            }
            else
            {
                roundResultText.text = "Cards are played face down, then revealed together.";
            }
        }

        private void RefreshHands()
        {
            ClearChildren(opponentHandArea);
            ClearChildren(playerHandArea);

            PlayerState topPlayer = activePlayer == PlayerId.PlayerB ? Session.PlayerA : Session.PlayerB;
            PlayerState bottomPlayer = activePlayer == PlayerId.PlayerB ? Session.PlayerB : Session.PlayerA;

            CreateHandHeader(opponentHandArea, topPlayer.DisplayName + " hand");
            CreateHandHeader(playerHandArea, bottomPlayer.DisplayName + " hand");

            bool activeCanSee = !waitingForPass && (Session.Phase == GamePhase.FirstPlayerSelecting || Session.Phase == GamePhase.SecondPlayerSelecting || swapSelectionMode);
            CreateHandCards(opponentHandArea, topPlayer, RevealAllHandsForPrototype || (activeCanSee && topPlayer.PlayerId == activePlayer), false);
            CreateHandCards(playerHandArea, bottomPlayer, RevealAllHandsForPrototype || (activeCanSee && bottomPlayer.PlayerId == activePlayer), activeCanSee && bottomPlayer.PlayerId == activePlayer);
        }

        private void RefreshPlayArea()
        {
            ClearChildren(playArea);
            if (currentRound == null)
            {
                return;
            }

            CreatePlayedSlot("First: " + PlayerName(currentRound.FirstPlayer), currentRound.FirstPlayedCard);
            CreatePlayedSlot("Second: " + PlayerName(currentRound.SecondPlayer), currentRound.SecondPlayedCard);
        }

        private void RefreshHistory()
        {
            ClearChildren(historyContent);
            CreateHistoryRow("Round History", 22, FontStyle.Bold);
            for (int i = 0; i < Session.RoundHistory.Count; i++)
            {
                RoundState round = Session.RoundHistory[i];
                string row = "R" + round.RoundIndex + " (" + round.RoundScore + "): A " + FindPlayerCardLabel(round, PlayerId.PlayerA) + " / B " + FindPlayerCardLabel(round, PlayerId.PlayerB) + " => " + (round.Winner == PlayerId.None ? "Tie" : PlayerName(round.Winner));
                CreateHistoryRow(row, 16, FontStyle.Normal);
            }
        }

        private void RefreshScorePanel(GameSessionState session)
        {
            if (scoreRows.Count == 0)
            {
                ClearChildren(scoreContent);
                Text header = BoardGameUiFactory.CreateText("ScoreHeader", scoreContent, "Round Scores", 20, TextAnchor.MiddleLeft, Color.white);
                RectTransform headerRect = header.transform as RectTransform;
                headerRect.sizeDelta = new Vector2(260f, 32f);
                for (int i = 0; i < RuleConfig.RoundScores.Length; i++)
                {
                    Text row = BoardGameUiFactory.CreateText("ScoreRow" + (i + 1), scoreContent, string.Empty, 17, TextAnchor.MiddleLeft, Color.white);
                    RectTransform rowRect = row.transform as RectTransform;
                    rowRect.sizeDelta = new Vector2(260f, 28f);
                    scoreRows.Add(row);
                }
            }

            for (int i = 0; i < scoreRows.Count; i++)
            {
                string suffix = string.Empty;
                if (session != null)
                {
                    RoundState round = session.RoundHistory.FirstOrDefault(item => item.RoundIndex == i + 1);
                    if (round != null)
                    {
                        suffix = round.Winner == PlayerId.None ? " - Tie" : " - " + PlayerName(round.Winner);
                    }
                    else if (session.CurrentRoundIndex == i + 1)
                    {
                        suffix = " - Current";
                    }
                }

                scoreRows[i].text = "R" + (i + 1) + ": " + RuleConfig.RoundScores[i] + suffix;
                scoreRows[i].color = session != null && session.CurrentRoundIndex == i + 1 ? new Color(1f, 0.86f, 0.25f, 1f) : Color.white;
            }
        }

        private void CreateHandHeader(Transform parent, string text)
        {
            Text header = BoardGameUiFactory.CreateText("Header", parent, text, 18, TextAnchor.MiddleCenter, Color.white);
            RectTransform rect = header.transform as RectTransform;
            rect.sizeDelta = new Vector2(150f, 128f);
        }

        private void CreateHandCards(Transform parent, PlayerState player, bool showNumbers, bool interactable)
        {
            for (int i = 0; i < player.HandCards.Count; i++)
            {
                CardData card = player.HandCards[i];
                CardView cardView = BoardGameUiFactory.CreateCardView(card.CardId, parent);
                cardView.Bind(card, showNumbers, interactable && card.Owner == activePlayer && card.Zone == CardZone.Hand, selectedCard == card, OnCardClicked);
            }
        }

        private void CreatePlayedSlot(string label, CardData card)
        {
            GameObject slot = BoardGameUiFactory.CreatePanel(label, playArea, new Color(0.11f, 0.15f, 0.18f, 0.82f));
            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(170f, 170f);
            VerticalLayoutGroup layout = slot.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 6f;
            layout.padding = new RectOffset(8, 8, 8, 8);

            Text text = BoardGameUiFactory.CreateText("SlotLabel", slot.transform, label, 15, TextAnchor.MiddleCenter, Color.white);
            (text.transform as RectTransform).sizeDelta = new Vector2(150f, 28f);
            if (card == null)
            {
                Text empty = BoardGameUiFactory.CreateText("Empty", slot.transform, "Waiting", 18, TextAnchor.MiddleCenter, new Color(0.72f, 0.75f, 0.78f, 1f));
                (empty.transform as RectTransform).sizeDelta = new Vector2(120f, 100f);
            }
            else
            {
                CardView cardView = BoardGameUiFactory.CreateCardView(card.CardId, slot.transform);
                bool showNumber = card.IsFaceUp || Session.Phase == GamePhase.RoundSettlement || Session.Phase == GamePhase.GameSettlement;
                cardView.Bind(card, showNumber, false, false, null);
            }
        }

        private void CreateHistoryRow(string text, int size, FontStyle style)
        {
            Text row = BoardGameUiFactory.CreateText("HistoryRow", historyContent, text, size, TextAnchor.MiddleLeft, Color.white);
            row.fontStyle = style;
            RectTransform rect = row.transform as RectTransform;
            rect.sizeDelta = new Vector2(420f, 28f);
        }

        private PlayerState GetPlayer(PlayerId playerId)
        {
            return playerId == PlayerId.PlayerA ? Session.PlayerA : Session.PlayerB;
        }

        private static string PlayerName(PlayerId playerId)
        {
            if (playerId == PlayerId.PlayerA)
            {
                return "Player A";
            }

            if (playerId == PlayerId.PlayerB)
            {
                return "Player B";
            }

            return "None";
        }

        private static string ColorLabel(CardColor color)
        {
            return color == CardColor.Red ? "Red" : color == CardColor.Green ? "Green" : "Blue";
        }

        private static string CardLabel(CardData card, bool showNumber)
        {
            if (card == null)
            {
                return "None";
            }

            return ColorLabel(card.Color) + " " + (showNumber ? card.Number.ToString() : "?");
        }

        private static string FindPlayerCardLabel(RoundState round, PlayerId playerId)
        {
            CardData card = round.FirstPlayedCard != null && round.FirstPlayedCard.Owner == playerId ? round.FirstPlayedCard : round.SecondPlayedCard;
            return CardLabel(card, true);
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private void LogMatchStart(DealResult dealResult)
        {
            Debug.Log("[BoardGame] Match start seed=" + RandomSeed + ", retry=" + dealResult.RetryCount + ", removed=" + dealResult.RemovedCard.CardId + ", A=" + string.Join(" ", dealResult.PlayerAHand.Select(card => card.CardId).ToArray()) + ", B=" + string.Join(" ", dealResult.PlayerBHand.Select(card => card.CardId).ToArray()));
        }

        private void BuildUiIfNeeded()
        {
            Canvas canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null)
            {
                if (BindExistingUi(canvas.transform))
                {
                    EnsureEventSystem();
                    return;
                }
            }

            GameObject canvasObject = new GameObject("BoardGameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject root = BoardGameUiFactory.CreatePanel("Root", canvasObject.transform, new Color(0.035f, 0.075f, 0.088f, 1f));
            BoardGameUiFactory.Stretch(root.GetComponent<RectTransform>());

            titleText = BoardGameUiFactory.CreateText("Title", root.transform, string.Empty, 34, TextAnchor.MiddleLeft, Color.white);
            BoardGameUiFactory.SetAnchor(titleText.transform as RectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -82f), new Vector2(-32f, -18f));

            GameObject topPanel = BoardGameUiFactory.CreatePanel("OpponentPanel", root.transform, new Color(0.06f, 0.12f, 0.15f, 0.86f));
            BoardGameUiFactory.SetAnchor(topPanel.GetComponent<RectTransform>(), new Vector2(0.03f, 0.72f), new Vector2(0.97f, 0.9f), Vector2.zero, Vector2.zero);
            opponentHandArea = topPanel.transform;
            HorizontalLayoutGroup topLayout = topPanel.AddComponent<HorizontalLayoutGroup>();
            topLayout.childAlignment = TextAnchor.MiddleCenter;
            topLayout.spacing = 18f;
            topLayout.padding = new RectOffset(18, 18, 10, 10);

            GameObject leftPanel = BoardGameUiFactory.CreatePanel("HistoryPanel", root.transform, new Color(0.04f, 0.09f, 0.11f, 0.86f));
            BoardGameUiFactory.SetAnchor(leftPanel.GetComponent<RectTransform>(), new Vector2(0.03f, 0.23f), new Vector2(0.27f, 0.69f), Vector2.zero, Vector2.zero);
            historyContent = leftPanel.transform;
            VerticalLayoutGroup historyLayout = leftPanel.AddComponent<VerticalLayoutGroup>();
            historyLayout.childAlignment = TextAnchor.UpperLeft;
            historyLayout.padding = new RectOffset(14, 14, 14, 14);
            historyLayout.spacing = 5f;

            GameObject centerPanel = BoardGameUiFactory.CreatePanel("CenterPanel", root.transform, new Color(0.08f, 0.14f, 0.14f, 0.78f));
            BoardGameUiFactory.SetAnchor(centerPanel.GetComponent<RectTransform>(), new Vector2(0.29f, 0.23f), new Vector2(0.72f, 0.69f), Vector2.zero, Vector2.zero);

            promptText = BoardGameUiFactory.CreateText("Prompt", centerPanel.transform, string.Empty, 22, TextAnchor.MiddleCenter, new Color(1f, 0.94f, 0.72f, 1f));
            BoardGameUiFactory.SetAnchor(promptText.transform as RectTransform, new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

            removedText = BoardGameUiFactory.CreateText("RemovedCard", centerPanel.transform, string.Empty, 20, TextAnchor.MiddleCenter, Color.white);
            BoardGameUiFactory.SetAnchor(removedText.transform as RectTransform, new Vector2(0.04f, 0.63f), new Vector2(0.96f, 0.77f), Vector2.zero, Vector2.zero);

            GameObject playAreaObject = new GameObject("PlayArea", typeof(RectTransform));
            playAreaObject.transform.SetParent(centerPanel.transform, false);
            playArea = playAreaObject.transform;
            BoardGameUiFactory.SetAnchor(playAreaObject.GetComponent<RectTransform>(), new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.62f), Vector2.zero, Vector2.zero);
            HorizontalLayoutGroup playLayout = playAreaObject.AddComponent<HorizontalLayoutGroup>();
            playLayout.childAlignment = TextAnchor.MiddleCenter;
            playLayout.spacing = 28f;

            roundResultText = BoardGameUiFactory.CreateText("RoundResult", centerPanel.transform, string.Empty, 19, TextAnchor.MiddleCenter, Color.white);
            BoardGameUiFactory.SetAnchor(roundResultText.transform as RectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.22f), Vector2.zero, Vector2.zero);

            GameObject rightPanel = BoardGameUiFactory.CreatePanel("ScorePanel", root.transform, new Color(0.04f, 0.09f, 0.11f, 0.86f));
            BoardGameUiFactory.SetAnchor(rightPanel.GetComponent<RectTransform>(), new Vector2(0.74f, 0.23f), new Vector2(0.97f, 0.69f), Vector2.zero, Vector2.zero);
            scoreContent = rightPanel.transform;
            VerticalLayoutGroup scoreLayout = rightPanel.AddComponent<VerticalLayoutGroup>();
            scoreLayout.childAlignment = TextAnchor.UpperLeft;
            scoreLayout.padding = new RectOffset(18, 18, 16, 16);
            scoreLayout.spacing = 4f;

            GameObject bottomPanel = BoardGameUiFactory.CreatePanel("PlayerPanel", root.transform, new Color(0.06f, 0.12f, 0.15f, 0.9f));
            BoardGameUiFactory.SetAnchor(bottomPanel.GetComponent<RectTransform>(), new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.21f), Vector2.zero, Vector2.zero);
            playerHandArea = bottomPanel.transform;
            HorizontalLayoutGroup bottomLayout = bottomPanel.AddComponent<HorizontalLayoutGroup>();
            bottomLayout.childAlignment = TextAnchor.MiddleCenter;
            bottomLayout.spacing = 18f;
            bottomLayout.padding = new RectOffset(18, 18, 8, 8);

            scoreText = BoardGameUiFactory.CreateText("ScoreText", root.transform, string.Empty, 24, TextAnchor.MiddleCenter, Color.white);
            BoardGameUiFactory.SetAnchor(scoreText.transform as RectTransform, new Vector2(0.34f, 0.9f), new Vector2(0.66f, 0.97f), Vector2.zero, Vector2.zero);

            phaseText = BoardGameUiFactory.CreateText("PhaseText", root.transform, string.Empty, 17, TextAnchor.MiddleRight, new Color(0.76f, 0.85f, 0.88f, 1f));
            BoardGameUiFactory.SetAnchor(phaseText.transform as RectTransform, new Vector2(0.54f, 0.93f), new Vector2(0.97f, 0.985f), Vector2.zero, Vector2.zero);

            GameObject buttonBar = new GameObject("ButtonBar", typeof(RectTransform));
            buttonBar.transform.SetParent(root.transform, false);
            BoardGameUiFactory.SetAnchor(buttonBar.GetComponent<RectTransform>(), new Vector2(0.28f, 0.02f), new Vector2(0.72f, 0.085f), Vector2.zero, Vector2.zero);
            HorizontalLayoutGroup buttonLayout = buttonBar.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.spacing = 12f;

            startButton = BoardGameUiFactory.CreateButton("StartButton", buttonBar.transform, "Start Match", new Color(0.23f, 0.52f, 0.36f, 1f));
            confirmButton = BoardGameUiFactory.CreateButton("ConfirmButton", buttonBar.transform, "Confirm Play", new Color(0.24f, 0.42f, 0.69f, 1f));
            passButton = BoardGameUiFactory.CreateButton("PassButton", buttonBar.transform, "I Am Ready", new Color(0.62f, 0.42f, 0.18f, 1f));
            noSwapButton = BoardGameUiFactory.CreateButton("NoSwapButton", buttonBar.transform, "No Swap", new Color(0.42f, 0.43f, 0.47f, 1f));
            swapModeButton = BoardGameUiFactory.CreateButton("SwapModeButton", buttonBar.transform, "Choose Swap", new Color(0.67f, 0.38f, 0.22f, 1f));
            confirmSwapButton = BoardGameUiFactory.CreateButton("ConfirmSwapButton", buttonBar.transform, "Confirm Swap", new Color(0.67f, 0.38f, 0.22f, 1f));
            SizeButton(startButton);
            SizeButton(confirmButton);
            SizeButton(passButton);
            SizeButton(noSwapButton);
            SizeButton(swapModeButton);
            SizeButton(confirmSwapButton);

            startButton.onClick.AddListener(StartNewGame);
            confirmButton.onClick.AddListener(ConfirmSelection);
            passButton.onClick.AddListener(ContinueAfterPass);
            noSwapButton.onClick.AddListener(ChooseNoSwap);
            swapModeButton.onClick.AddListener(EnterSwapMode);
            confirmSwapButton.onClick.AddListener(ConfirmSelection);

            settlementPanel = BoardGameUiFactory.CreatePanel("GameResultPopup", root.transform, new Color(0.02f, 0.025f, 0.03f, 0.94f));
            BoardGameUiFactory.SetAnchor(settlementPanel.GetComponent<RectTransform>(), new Vector2(0.32f, 0.2f), new Vector2(0.68f, 0.82f), Vector2.zero, Vector2.zero);
            settlementText = BoardGameUiFactory.CreateText("SettlementText", settlementPanel.transform, string.Empty, 24, TextAnchor.MiddleCenter, Color.white);
            BoardGameUiFactory.Stretch(settlementText.transform as RectTransform);

            Button restartButton = BoardGameUiFactory.CreateButton("RestartButton", settlementPanel.transform, "Play Again", new Color(0.23f, 0.52f, 0.36f, 1f));
            BoardGameUiFactory.SetAnchor(restartButton.transform as RectTransform, new Vector2(0.28f, 0.04f), new Vector2(0.72f, 0.14f), Vector2.zero, Vector2.zero);
            restartButton.onClick.AddListener(StartNewGame);
            settlementPanel.SetActive(false);
            EnsureEventSystem();
        }

        private static void SizeButton(Button button)
        {
            RectTransform rect = button.transform as RectTransform;
            rect.sizeDelta = new Vector2(150f, 50f);
        }

        private static void SetButtonText(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = text;
            }
        }

        private void EnsureEventSystem()
        {
            EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            GameObject eventSystemObject;
            if (eventSystem == null)
            {
                eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }
            else
            {
                eventSystemObject = eventSystem.gameObject;
            }

            if (eventSystemObject.GetComponent<BaseInputModule>() != null)
            {
                return;
            }

            Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null)
            {
                Component module = eventSystemObject.AddComponent(inputSystemModuleType);
                System.Reflection.MethodInfo assignDefaultActions = inputSystemModuleType.GetMethod("AssignDefaultActions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (assignDefaultActions != null)
                {
                    assignDefaultActions.Invoke(module, null);
                }

                return;
            }

            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private bool BindExistingUi(Transform canvasTransform)
        {
            Transform root = canvasTransform.Find("Root");
            if (root == null)
            {
                return false;
            }

            titleText = FindChildComponent<Text>(root, "Title");
            promptText = FindChildComponent<Text>(root, "CenterPanel/Prompt");
            scoreText = FindChildComponent<Text>(root, "ScoreText");
            removedText = FindChildComponent<Text>(root, "CenterPanel/RemovedCard");
            phaseText = FindChildComponent<Text>(root, "PhaseText");
            roundResultText = FindChildComponent<Text>(root, "CenterPanel/RoundResult");
            opponentHandArea = FindChild(root, "OpponentPanel");
            playerHandArea = FindChild(root, "PlayerPanel");
            playArea = FindChild(root, "CenterPanel/PlayArea");
            historyContent = FindChild(root, "HistoryPanel");
            scoreContent = FindChild(root, "ScorePanel");
            startButton = FindChildComponent<Button>(root, "ButtonBar/StartButton");
            confirmButton = FindChildComponent<Button>(root, "ButtonBar/ConfirmButton");
            passButton = FindChildComponent<Button>(root, "ButtonBar/PassButton");
            noSwapButton = FindChildComponent<Button>(root, "ButtonBar/NoSwapButton");
            swapModeButton = FindChildComponent<Button>(root, "ButtonBar/SwapModeButton");
            confirmSwapButton = FindChildComponent<Button>(root, "ButtonBar/ConfirmSwapButton");
            settlementPanel = FindChild(root, "GameResultPopup") != null ? FindChild(root, "GameResultPopup").gameObject : null;
            settlementText = FindChildComponent<Text>(root, "GameResultPopup/SettlementText");

            bool hasRequiredReferences =
                titleText != null && promptText != null && scoreText != null && removedText != null && phaseText != null &&
                roundResultText != null && opponentHandArea != null && playerHandArea != null && playArea != null &&
                historyContent != null && scoreContent != null && startButton != null && confirmButton != null &&
                passButton != null && noSwapButton != null && swapModeButton != null && confirmSwapButton != null &&
                settlementPanel != null && settlementText != null;

            if (!hasRequiredReferences)
            {
                return false;
            }

            startButton.onClick.RemoveAllListeners();
            confirmButton.onClick.RemoveAllListeners();
            passButton.onClick.RemoveAllListeners();
            noSwapButton.onClick.RemoveAllListeners();
            swapModeButton.onClick.RemoveAllListeners();
            confirmSwapButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartNewGame);
            confirmButton.onClick.AddListener(ConfirmSelection);
            passButton.onClick.AddListener(ContinueAfterPass);
            noSwapButton.onClick.AddListener(ChooseNoSwap);
            swapModeButton.onClick.AddListener(EnterSwapMode);
            confirmSwapButton.onClick.AddListener(ConfirmSelection);

            Button restartButton = FindChildComponent<Button>(root, "GameResultPopup/RestartButton");
            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(StartNewGame);
            }

            scoreRows.Clear();
            for (int i = 0; i < RuleConfig.RoundScores.Length; i++)
            {
                Text row = FindChildComponent<Text>(root, "ScorePanel/ScoreRow" + (i + 1));
                if (row != null)
                {
                    scoreRows.Add(row);
                }
            }

            return true;
        }

        private static Transform FindChild(Transform root, string path)
        {
            return root != null ? root.Find(path) : null;
        }

        private static T FindChildComponent<T>(Transform root, string path) where T : Component
        {
            Transform child = FindChild(root, path);
            return child != null ? child.GetComponent<T>() : null;
        }
    }
}
