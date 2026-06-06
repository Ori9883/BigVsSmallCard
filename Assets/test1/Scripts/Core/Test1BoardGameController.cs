using System;
using UnityEngine;
using UnityEngine.Events;

namespace Test1.BoardGame
{
    [Serializable]
    public sealed class Test1GameStateEvent : UnityEvent<Test1GameState>
    {
    }

    [Serializable]
    public sealed class Test1StringEvent : UnityEvent<string>
    {
    }

    public sealed class Test1BoardGameController : MonoBehaviour
    {
        [Header("Rules")]
        public Test1GameRuleConfig RuleConfig;
        public bool UseRandomSeed = true;
        public int RandomSeed;
        public bool StartOnAwake;

        [Header("Runtime")]
        [SerializeField] private Test1GameState state = new Test1GameState();
        [SerializeField] private string selectedCardId;

        public Test1GameStateEvent OnStateChanged = new Test1GameStateEvent();
        public Test1StringEvent OnSelectionChanged = new Test1StringEvent();
        public Test1StringEvent OnMessage = new Test1StringEvent();

        private readonly Test1BoardGameSession session = new Test1BoardGameSession();

        public Test1GameState State
        {
            get { return state; }
        }

        public string SelectedCardId
        {
            get { return selectedCardId; }
        }

        private void Awake()
        {
            if (RuleConfig == null)
            {
                RuleConfig = Test1GameRuleConfig.CreateRuntimeDefault();
            }

            session.LogGenerated += PublishMessage;
        }

        private void Start()
        {
            if (StartOnAwake)
            {
                StartNewGame();
            }
            else
            {
                PublishState();
            }
        }

        private void OnDestroy()
        {
            session.LogGenerated -= PublishMessage;
        }

        public void StartNewGame()
        {
            RunCommand(() =>
            {
                // A fixed RandomSeed can replay the exact same deal for debugging.
                ClearSelection(false);
                int seed = UseRandomSeed || RandomSeed == 0 ? Environment.TickCount : RandomSeed;
                RandomSeed = seed;
                session.StartNewGame(seed, RuleConfig);
                state = session.State;
                PublishState();
            });
        }

        public void SelectCardById(string cardId)
        {
            // Card views only pass ids; the session verifies ownership and phase before accepting input.
            if (state == null || string.IsNullOrEmpty(cardId))
            {
                return;
            }

            if (selectedCardId == cardId)
            {
                ClearSelection(true);
                return;
            }

            if (!session.CanUseCard(state.CurrentActor, cardId))
            {
                PublishMessage("Card cannot be selected now: " + cardId);
                return;
            }

            selectedCardId = cardId;
            OnSelectionChanged.Invoke(selectedCardId);
            PublishState();
        }

        public void ConfirmSelectedCard()
        {
            RunCommand(() =>
            {
                if (string.IsNullOrEmpty(selectedCardId))
                {
                    PublishMessage("Select a card before confirming.");
                    return;
                }

                if (state.Phase == Test1GamePhase.WaitingForSwapCard)
                {
                    ConfirmSwapSelectedCard();
                    return;
                }

                session.PlayCard(state.CurrentActor, selectedCardId);
                state = session.State;
                ClearSelection(false);
                PublishState();
            });
        }

        public void ContinueAfterRound()
        {
            RunCommand(() =>
            {
                session.ContinueAfterRoundSettlement();
                state = session.State;
                ClearSelection(false);
                PublishState();
            });
        }

        public void KeepRemovedCard()
        {
            RunCommand(() =>
            {
                session.KeepRemovedCard(state.CurrentActor);
                state = session.State;
                ClearSelection(false);
                PublishState();
            });
        }

        public void EnterSwapSelection()
        {
            RunCommand(() =>
            {
                session.EnterSwapSelection(state.CurrentActor);
                state = session.State;
                ClearSelection(false);
                PublishState();
            });
        }

        public void ConfirmSwapSelectedCard()
        {
            RunCommand(() =>
            {
                if (string.IsNullOrEmpty(selectedCardId))
                {
                    PublishMessage("Select a hand card to swap.");
                    return;
                }

                session.SwapRemovedWithHandCard(state.CurrentActor, selectedCardId);
                state = session.State;
                ClearSelection(false);
                PublishState();
            });
        }

        public bool CanConfirmSelectedCard()
        {
            return !string.IsNullOrEmpty(selectedCardId)
                && state != null
                && session.CanUseCard(state.CurrentActor, selectedCardId);
        }

        private void ClearSelection(bool publish)
        {
            selectedCardId = null;
            OnSelectionChanged.Invoke(string.Empty);
            if (publish)
            {
                PublishState();
            }
        }

        private void RunCommand(Action command)
        {
            try
            {
                command();
            }
            catch (Exception exception)
            {
                PublishMessage(exception.Message);
            }
        }

        private void PublishState()
        {
            OnStateChanged.Invoke(state);
        }

        private void PublishMessage(string message)
        {
            Debug.Log(message);
            OnMessage.Invoke(message);
        }
    }
}
