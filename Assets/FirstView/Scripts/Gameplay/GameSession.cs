using System.Collections.Generic;
using UnityEngine;

namespace FirstView.Gameplay
{
    public readonly struct SettledRoundRecord
    {
        public readonly int Round;
        public readonly GameCard PlayerCard;
        public readonly GameCard EnemyCard;
        public readonly int Result;
        public readonly int Score;

        public SettledRoundRecord(int round, GameCard playerCard, GameCard enemyCard, int result, int score)
        {
            Round = round;
            PlayerCard = playerCard;
            EnemyCard = enemyCard;
            Result = result;
            Score = score;
        }
    }

    public class GameSession : MonoBehaviour
    {
        public List<GameCard> PlayerHand { get; private set; }
        public List<GameCard> EnemyHand { get; private set; }
        public List<SettledRoundRecord> SettledHistory { get; private set; }
        public GameCard RemovedCard { get; private set; }
        public int PlayerScore { get; private set; }
        public int EnemyScore { get; private set; }
        public int CurrentRound { get; private set; }
        public RoundPhase Phase { get; private set; }
        public bool PlayerIsFirst => CurrentRound % 2 == 1;

        public int PlayerPlayedIndex { get; private set; } = -1;
        public int EnemyPlayedIndex { get; private set; } = -1;

        public System.Action OnRoundStart;
        public System.Action<bool> OnTurnStart;
        public System.Action OnBothCardsPlayed;
        public System.Action<int, int, int> OnSettled;
        public System.Action<int, int> OnGameOver;

        public void BeginGame()
        {
            Phase = RoundPhase.Dealing;

            if (!Deck.Deal(out var ph, out var eh, out var removed))
            {
                Debug.LogError("[GameSession] Deal failed");
                return;
            }

            PlayerHand = ph;
            EnemyHand = eh;
            SettledHistory = new List<SettledRoundRecord>(ScoreSystem.TotalRounds);
            RemovedCard = removed;

            PlayerScore = 0;
            EnemyScore = 0;
            CurrentRound = 0;

            LogHands();
            StartNextRound();
        }

        private void LogHands()
        {
            string p = "Player: ";
            for (int i = 0; i < PlayerHand.Count; i++) p += PlayerHand[i] + " ";
            string e = "Enemy: ";
            for (int i = 0; i < EnemyHand.Count; i++) e += EnemyHand[i] + " ";
            Debug.Log($"[GameSession] Removed={RemovedCard}\n{p}\n{e}");
        }

        private void StartNextRound()
        {
            CurrentRound++;
            if (CurrentRound > ScoreSystem.TotalRounds)
            {
                EndGame();
                return;
            }

            PlayerPlayedIndex = -1;
            EnemyPlayedIndex = -1;
            Phase = RoundPhase.RoundStart;
            Debug.Log($"[GameSession] === Round {CurrentRound} === PlayerIsFirst={PlayerIsFirst}");
            OnRoundStart?.Invoke();

            Phase = RoundPhase.FirstTurn;
            OnTurnStart?.Invoke(PlayerIsFirst);
        }

        public void OnCardPlayed(bool isPlayer)
        {
            if (Phase == RoundPhase.FirstTurn)
            {
                Phase = RoundPhase.SecondTurn;
                OnTurnStart?.Invoke(!isPlayer);
            }
            else if (Phase == RoundPhase.SecondTurn)
            {
                Phase = RoundPhase.Reveal;
                OnBothCardsPlayed?.Invoke();
            }
            else
            {
                Debug.LogError($"[GameSession] OnCardPlayed({isPlayer}) called in unexpected Phase={Phase}");
            }
        }

        public void SetPlayedIndex(bool isPlayer, int handIndex)
        {
            if (isPlayer) PlayerPlayedIndex = handIndex;
            else EnemyPlayedIndex = handIndex;
        }

        public void Settle()
        {
            Phase = RoundPhase.Settlement;

            GameCard playerCard = PlayerHand[PlayerPlayedIndex];
            GameCard enemyCard = EnemyHand[EnemyPlayedIndex];
            int pNum = playerCard.Number;
            int eNum = enemyCard.Number;
            int result = ScoreSystem.Compare(pNum, eNum);
            int score = ScoreSystem.GetRoundScore(CurrentRound - 1);

            string winner;
            if (result == ScoreSystem.PlayerWin)
            {
                PlayerScore += score;
                winner = "Player";
            }
            else if (result == ScoreSystem.EnemyWin)
            {
                EnemyScore += score;
                winner = "Enemy";
            }
            else
            {
                winner = "Draw";
            }

            Debug.Log($"[Settlement] Round {CurrentRound}: P({pNum}) vs E({eNum}) -> {winner} (+{score}) | Total P={PlayerScore} E={EnemyScore}");

            SettledHistory.Add(new SettledRoundRecord(CurrentRound, playerCard, enemyCard, result, score));

            PlayerHand.RemoveAt(PlayerPlayedIndex);
            EnemyHand.RemoveAt(EnemyPlayedIndex);

            OnSettled?.Invoke(result, score, CurrentRound);

            StartNextRound();
        }

        private void EndGame()
        {
            Phase = RoundPhase.GameOver;
            string resultStr;
            if (PlayerScore > EnemyScore) resultStr = "Player Wins";
            else if (EnemyScore > PlayerScore) resultStr = "Enemy Wins";
            else resultStr = "Draw";

            Debug.Log($"[GameSession] Game Over! Player={PlayerScore} Enemy={EnemyScore} -> {resultStr}");
            OnGameOver?.Invoke(PlayerScore, EnemyScore);
        }
    }
}
