using System;
using System.Collections.Generic;
using System.Linq;

namespace Test1.BoardGame
{
    public sealed class Test1BoardGameSession
    {
        private Test1GameRuleConfig config;
        private Test1TurnManager turnManager;

        // Pure gameplay session. It owns the authoritative state and never creates UI objects.
        public Test1GameState State { get; private set; } = new Test1GameState();
        public event Action<string> LogGenerated;

        public void StartNewGame(int seed, Test1GameRuleConfig ruleConfig)
        {
            config = ruleConfig == null ? Test1GameRuleConfig.CreateRuntimeDefault() : ruleConfig;
            config.EnsureValidDefaults();

            System.Random runtimeRandom = new System.Random(seed);
            turnManager = new Test1TurnManager(config.TurnPolicy, runtimeRandom);
            Test1DealResult dealResult = Test1DealService.Deal(seed, config);

            State = new Test1GameState
            {
                RandomSeed = seed,
                DealRetryCount = dealResult.RetryCount,
                Phase = Test1GamePhase.Dealing,
                CurrentRoundIndex = 0,
                CurrentActor = Test1PlayerId.None,
                PlayerA = new Test1PlayerState(Test1PlayerId.PlayerA, "Player A"),
                PlayerB = new Test1PlayerState(Test1PlayerId.PlayerB, "Player B"),
                RemovedCard = dealResult.RemovedCard,
                AllCards = dealResult.AllCards,
                PeekOwner = Test1PlayerId.None,
                HasPeekResolved = false,
                LastRoundFirstPlayer = Test1PlayerId.None,
                LastRoundWinner = Test1PlayerId.None,
                GameResult = Test1GameResultType.None,
                FinalWinner = Test1PlayerId.None
            };

            State.PlayerA.HandCards = dealResult.PlayerAHand;
            State.PlayerB.HandCards = dealResult.PlayerBHand;

            EmitLog("Match start seed=" + seed + ", retry=" + dealResult.RetryCount + ", removed=" + dealResult.RemovedCard.CardId);
            BeginNextRound();
        }

        public IReadOnlyList<Test1Card> GetLegalCards(Test1PlayerId playerId)
        {
            Test1PlayerState player = State.GetPlayer(playerId);
            return player == null ? Array.Empty<Test1Card>() : player.HandCards;
        }

        public bool CanUseCard(Test1PlayerId playerId, string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || State.CurrentActor != playerId)
            {
                return false;
            }

            if (!IsCardSelectionPhase(State.Phase))
            {
                return false;
            }

            return FindHandCard(playerId, cardId) != null;
        }

        public void PlayCard(Test1PlayerId playerId, string cardId)
        {
            // Both players commit cards face-down first; numbers become public only in RevealAndSettleRound.
            if (State.Phase != Test1GamePhase.WaitingForFirstPlayer && State.Phase != Test1GamePhase.WaitingForSecondPlayer)
            {
                throw new InvalidOperationException("The game is not waiting for a played card.");
            }

            if (State.CurrentActor != playerId)
            {
                throw new InvalidOperationException("It is not " + playerId + "'s turn.");
            }

            Test1PlayerState player = State.GetPlayer(playerId);
            Test1Card card = FindHandCard(playerId, cardId);
            if (card == null)
            {
                throw new InvalidOperationException("Card is not in the active player's hand: " + cardId);
            }

            player.HandCards.Remove(card);
            card.Owner = playerId;
            card.Zone = Test1CardZone.Played;
            card.IsFaceUp = false;
            card.PlayedRoundIndex = State.CurrentRoundIndex;

            if (State.Phase == Test1GamePhase.WaitingForFirstPlayer)
            {
                State.CurrentRound.FirstPlayedCard = card;
                State.CurrentActor = State.CurrentRound.SecondPlayer;
                State.Phase = Test1GamePhase.WaitingForSecondPlayer;
                EmitLog(PlayerName(playerId) + " played a face-down " + card.Color + " card.");
                return;
            }

            State.CurrentRound.SecondPlayedCard = card;
            EmitLog(PlayerName(playerId) + " played a face-down " + card.Color + " card.");
            RevealAndSettleRound();
        }

        public void ContinueAfterRoundSettlement()
        {
            // The peek-and-swap window must happen exactly once after round 3 settlement.
            if (State.Phase != Test1GamePhase.RoundSettlement)
            {
                return;
            }

            if (State.CurrentRoundIndex == 3 && config.EnablePeekSwap && !State.HasPeekResolved)
            {
                ResolvePeekDecisionPoint();
                return;
            }

            if (State.CurrentRoundIndex >= config.RoundCount)
            {
                CompleteGame();
                return;
            }

            BeginNextRound();
        }

        public Test1Card PeekRemovedCard(Test1PlayerId playerId)
        {
            EnsurePeekActor(playerId);
            return State.RemovedCard;
        }

        public void KeepRemovedCard(Test1PlayerId playerId)
        {
            EnsurePeekActor(playerId);
            State.HasPeekResolved = true;
            State.SwapOccurred = false;
            State.CurrentActor = Test1PlayerId.None;
            EmitLog(PlayerName(playerId) + " kept the removed card unchanged.");
            BeginNextRound();
        }

        public void EnterSwapSelection(Test1PlayerId playerId)
        {
            EnsurePeekActor(playerId);
            State.Phase = Test1GamePhase.WaitingForSwapCard;
            State.CurrentActor = playerId;
        }

        public void SwapRemovedWithHandCard(Test1PlayerId playerId, string handCardId)
        {
            // Swapping preserves hand size: one hidden removed card enters hand, one hand card becomes removed.
            if (State.Phase != Test1GamePhase.WaitingForSwapCard)
            {
                throw new InvalidOperationException("The game is not waiting for a swap card.");
            }

            if (State.CurrentActor != playerId || State.PeekOwner != playerId)
            {
                throw new InvalidOperationException("Only the peek owner can swap the removed card.");
            }

            Test1PlayerState player = State.GetPlayer(playerId);
            Test1Card selectedHandCard = FindHandCard(playerId, handCardId);
            if (selectedHandCard == null)
            {
                throw new InvalidOperationException("Swap card is not in hand: " + handCardId);
            }

            Test1Card oldRemovedCard = State.RemovedCard;
            player.HandCards.Remove(selectedHandCard);

            selectedHandCard.Owner = Test1PlayerId.None;
            selectedHandCard.Zone = Test1CardZone.Removed;
            selectedHandCard.IsFaceUp = false;
            selectedHandCard.PlayedRoundIndex = 0;

            oldRemovedCard.Owner = playerId;
            oldRemovedCard.Zone = Test1CardZone.Hand;
            oldRemovedCard.IsFaceUp = false;
            oldRemovedCard.PlayedRoundIndex = 0;
            player.HandCards.Add(oldRemovedCard);
            SortHand(player.HandCards);

            State.RemovedCard = selectedHandCard;
            State.HasPeekResolved = true;
            State.SwapOccurred = true;
            State.SwapPlayer = playerId;
            State.SwappedOutCardId = selectedHandCard.CardId;
            State.SwappedInCardId = oldRemovedCard.CardId;
            State.CurrentActor = Test1PlayerId.None;

            EmitLog(PlayerName(playerId) + " swapped in " + oldRemovedCard.CardId + " and removed " + selectedHandCard.CardId + ".");
            BeginNextRound();
        }

        private void BeginNextRound()
        {
            // CurrentRoundIndex is 1-based to match the table rule document and UI copy.
            if (State.CurrentRoundIndex >= config.RoundCount)
            {
                CompleteGame();
                return;
            }

            State.CurrentRoundIndex++;
            Test1PlayerId firstPlayer = turnManager.GetFirstPlayer(State.CurrentRoundIndex, State.LastRoundFirstPlayer, State.LastRoundWinner);
            Test1PlayerId secondPlayer = Test1GameState.OpponentOf(firstPlayer);

            State.CurrentRound = new Test1RoundState
            {
                RoundIndex = State.CurrentRoundIndex,
                RoundScore = config.GetRoundScore(State.CurrentRoundIndex),
                FirstPlayer = firstPlayer,
                SecondPlayer = secondPlayer,
                Winner = Test1PlayerId.None,
                ResultType = Test1RoundResultType.Tie
            };

            State.LastRoundFirstPlayer = firstPlayer;
            State.CurrentActor = firstPlayer;
            State.Phase = Test1GamePhase.WaitingForFirstPlayer;
            EmitLog("Round " + State.CurrentRoundIndex + " starts. First player: " + PlayerName(firstPlayer) + ".");
        }

        private void RevealAndSettleRound()
        {
            // All score changes are applied from the rule engine so UI remains presentation-only.
            State.Phase = Test1GamePhase.Reveal;
            State.CurrentActor = Test1PlayerId.None;
            State.CurrentRound.FirstPlayedCard.IsFaceUp = true;
            State.CurrentRound.SecondPlayedCard.IsFaceUp = true;

            Test1RuleEngine.ApplyRoundResult(State.CurrentRound, State.PlayerA, State.PlayerB);
            State.LastRoundWinner = State.CurrentRound.Winner;
            State.RoundHistory.Add(State.CurrentRound);

            MovePlayedCardToDiscard(State.CurrentRound.FirstPlayedCard);
            MovePlayedCardToDiscard(State.CurrentRound.SecondPlayedCard);

            State.Phase = Test1GamePhase.RoundSettlement;
            EmitLog("Round " + State.CurrentRound.RoundIndex + " result: " + DescribeRoundWinner(State.CurrentRound) + ".");
        }

        private void MovePlayedCardToDiscard(Test1Card card)
        {
            card.Zone = Test1CardZone.Discard;
            Test1PlayerState player = State.GetPlayer(card.Owner);
            if (player != null)
            {
                player.DiscardCards.Add(card);
            }
        }

        private void ResolvePeekDecisionPoint()
        {
            State.PeekOwner = Test1RuleEngine.ResolvePeekOwner(State.RoundHistory);
            if (State.PeekOwner == Test1PlayerId.None)
            {
                State.HasPeekResolved = true;
                EmitLog("No player gained peek right after round 3.");
                BeginNextRound();
                return;
            }

            State.Phase = Test1GamePhase.PeekDecision;
            State.CurrentActor = State.PeekOwner;
            EmitLog(PlayerName(State.PeekOwner) + " gained peek right.");
        }

        private void CompleteGame()
        {
            Test1PlayerId winner;
            State.GameResult = Test1RuleEngine.ResolveGameResult(State.PlayerA, State.PlayerB, out winner);
            State.FinalWinner = winner;
            State.CurrentActor = Test1PlayerId.None;
            State.CurrentRound = null;
            State.Phase = Test1GamePhase.GameOver;
            EmitLog("Game over. Result=" + State.GameResult + ", score A=" + State.PlayerA.Score + ", score B=" + State.PlayerB.Score + ".");
        }

        private Test1Card FindHandCard(Test1PlayerId playerId, string cardId)
        {
            Test1PlayerState player = State.GetPlayer(playerId);
            return player == null ? null : player.HandCards.FirstOrDefault(card => card.CardId == cardId);
        }

        private void EnsurePeekActor(Test1PlayerId playerId)
        {
            if (State.Phase != Test1GamePhase.PeekDecision && State.Phase != Test1GamePhase.WaitingForSwapCard)
            {
                throw new InvalidOperationException("The game is not in peek or swap phase.");
            }

            if (State.PeekOwner != playerId)
            {
                throw new InvalidOperationException("Only the peek owner can perform this action.");
            }
        }

        private static bool IsCardSelectionPhase(Test1GamePhase phase)
        {
            return phase == Test1GamePhase.WaitingForFirstPlayer
                || phase == Test1GamePhase.WaitingForSecondPlayer
                || phase == Test1GamePhase.WaitingForSwapCard;
        }

        private static void SortHand(List<Test1Card> hand)
        {
            hand.Sort((left, right) =>
            {
                int colorCompare = left.Color.CompareTo(right.Color);
                return colorCompare != 0 ? colorCompare : left.Number.CompareTo(right.Number);
            });
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

        private static string DescribeRoundWinner(Test1RoundState round)
        {
            return round.Winner == Test1PlayerId.None ? "tie" : PlayerName(round.Winner) + " wins " + round.RoundScore;
        }

        private void EmitLog(string message)
        {
            LogGenerated?.Invoke("[Test1BoardGame] " + message);
        }
    }
}
