using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FirstView.Gameplay
{
    public enum EnemyAIDifficulty
    {
        Normal,
        Strong,
        God
    }

    public readonly struct PublicCardInfo
    {
        public readonly CardColor Color;

        public PublicCardInfo(CardColor color)
        {
            Color = color;
        }
    }

    public readonly struct EnemyAIDecisionContext
    {
        public readonly EnemyAIDifficulty Difficulty;
        public readonly List<PublicCardInfo> PlayerHandPublicCards;
        public readonly bool HasPlayerPlayedPublicCard;
        public readonly PublicCardInfo PlayerPlayedPublicCard;
        public readonly List<SettledRoundRecord> SettledHistory;
        public readonly GameCard RemovedCard;
        public readonly int CurrentRound;
        public readonly int PlayerScore;
        public readonly int EnemyScore;
        public readonly bool PlayerIsFirst;

        public EnemyAIDecisionContext(
            EnemyAIDifficulty difficulty,
            List<PublicCardInfo> playerHandPublicCards,
            bool hasPlayerPlayedPublicCard,
            PublicCardInfo playerPlayedPublicCard,
            List<SettledRoundRecord> settledHistory,
            GameCard removedCard,
            int currentRound,
            int playerScore,
            int enemyScore,
            bool playerIsFirst)
        {
            Difficulty = difficulty;
            PlayerHandPublicCards = playerHandPublicCards;
            HasPlayerPlayedPublicCard = hasPlayerPlayedPublicCard;
            PlayerPlayedPublicCard = playerPlayedPublicCard;
            SettledHistory = settledHistory;
            RemovedCard = removedCard;
            CurrentRound = currentRound;
            PlayerScore = playerScore;
            EnemyScore = enemyScore;
            PlayerIsFirst = playerIsFirst;
        }

        public bool EnemyActsSecond => PlayerIsFirst && HasPlayerPlayedPublicCard;
    }

    public static class EnemyAI
    {
        private const float RandomTieRange = 0.0001f;

        public static GameCard PickCard(List<GameCard> hand)
        {
            if (hand == null || hand.Count == 0) return default;
            int idx = Random.Range(0, hand.Count);
            return hand[idx];
        }

        public static int PickCardIndex(List<GameCard> hand)
        {
            if (hand == null || hand.Count == 0) return -1;
            return Random.Range(0, hand.Count);
        }

        public static int PickCardIndex(List<GameCard> hand, EnemyAIDecisionContext context)
        {
            if (hand == null || hand.Count == 0) return -1;

            List<GameCard> inferredPlayerCards = BuildInferredPlayerCards(hand, context);
            int bestIndex = 0;
            float bestScore = float.NegativeInfinity;
            StringBuilder candidates = ShouldLogAI() ? new StringBuilder() : null;

            for (int i = 0; i < hand.Count; i++)
            {
                GameCard candidate = hand[i];
                float score = ScoreCandidate(candidate, hand, inferredPlayerCards, context);
                score += Random.Range(0f, RandomTieRange);

                if (candidates != null)
                {
                    if (candidates.Length > 0) candidates.Append(" | ");
                    candidates.Append(ShortCardName(candidate)).Append('=').Append(score.ToString("0.0"));
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            LogDecision(context, hand[bestIndex], bestScore, candidates, inferredPlayerCards.Count);
            return bestIndex;
        }

        private static float ScoreCandidate(GameCard candidate, List<GameCard> hand, List<GameCard> inferredPlayerCards, EnemyAIDecisionContext context)
        {
            switch (context.Difficulty)
            {
                case EnemyAIDifficulty.God:
                    return ScoreGod(candidate, hand, inferredPlayerCards, context);
                case EnemyAIDifficulty.Strong:
                    return ScoreStrong(candidate, hand, inferredPlayerCards, context);
                default:
                    return ScoreNormal(candidate, inferredPlayerCards, context);
            }
        }

        private static float ScoreNormal(GameCard candidate, List<GameCard> inferredPlayerCards, EnemyAIDecisionContext context)
        {
            float inferredWinRate = EstimateWinRate(candidate, inferredPlayerCards, 0.2f);
            float numberValue = candidate.Number == 1 ? 3.5f : candidate.Number;
            float roundWeight = Mathf.Clamp01(GetCurrentRoundScore(context) / 30f);
            float historyAwareness = Random.value < 0.35f ? inferredWinRate * 5f : 0f;
            float secondTurnCaution = context.EnemyActsSecond ? inferredWinRate * 4f : 0f;
            return numberValue * Mathf.Lerp(0.7f, 1.1f, roundWeight) + historyAwareness + secondTurnCaution + Random.Range(0f, 6f);
        }

        private static float ScoreStrong(GameCard candidate, List<GameCard> hand, List<GameCard> inferredPlayerCards, EnemyAIDecisionContext context)
        {
            float roundScore = GetCurrentRoundScore(context);
            float pressure = GetScorePressure(context);
            float preservation = GetPreservationValue(candidate, hand, context, 1.6f);
            float winRate = EstimateWinRate(candidate, inferredPlayerCards, 0.35f);
            float value = GetCardPower(candidate) * 4f;
            float secondTurnWeight = context.EnemyActsSecond ? 12f : 0f;
            return winRate * (30f + roundScore + secondTurnWeight) + value + pressure - preservation;
        }

        private static float ScoreGod(GameCard candidate, List<GameCard> hand, List<GameCard> inferredPlayerCards, EnemyAIDecisionContext context)
        {
            float roundScore = GetCurrentRoundScore(context);
            float futureValue = EstimateFutureHandValueAfterPlay(candidate, hand);
            float scoreSwingNeed = GetScorePressure(context) * 1.4f;
            float winRate = EstimateWinRate(candidate, inferredPlayerCards, 0.4f);
            float denyValue = EstimatePlayerThreat(inferredPlayerCards) * 0.35f;
            float preservation = GetPreservationValue(candidate, hand, context, 2.4f);
            float secondTurnWeight = context.EnemyActsSecond ? 20f : 0f;
            return winRate * (55f + roundScore * 1.4f + secondTurnWeight) + futureValue + denyValue + scoreSwingNeed - preservation;
        }

        private static List<GameCard> BuildInferredPlayerCards(List<GameCard> enemyHand, EnemyAIDecisionContext context)
        {
            List<GameCard> pool = Deck.CreateFullDeck();
            RemoveKnownCard(pool, context.RemovedCard);

            for (int i = 0; i < enemyHand.Count; i++)
                RemoveKnownCard(pool, enemyHand[i]);

            float historyUseRate = GetHistoryUseRate(context.Difficulty);
            if (context.SettledHistory != null && historyUseRate > 0f)
            {
                for (int i = 0; i < context.SettledHistory.Count; i++)
                {
                    if (Random.value > historyUseRate) continue;
                    RemoveKnownCard(pool, context.SettledHistory[i].PlayerCard);
                    RemoveKnownCard(pool, context.SettledHistory[i].EnemyCard);
                }
            }

            if (context.EnemyActsSecond)
                return FilterBySinglePublicColor(pool, context.PlayerPlayedPublicCard.Color);

            return FilterByPublicHandColors(pool, context.PlayerHandPublicCards);
        }

        private static float GetHistoryUseRate(EnemyAIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case EnemyAIDifficulty.God: return 1f;
                case EnemyAIDifficulty.Strong: return 0.85f;
                default: return 0.35f;
            }
        }

        private static List<GameCard> FilterBySinglePublicColor(List<GameCard> pool, CardColor color)
        {
            List<GameCard> result = new List<GameCard>();
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].Color == color)
                    result.Add(pool[i]);
            }
            return result.Count > 0 ? result : pool;
        }

        private static List<GameCard> FilterByPublicHandColors(List<GameCard> pool, List<PublicCardInfo> publicCards)
        {
            if (publicCards == null || publicCards.Count == 0) return pool;

            List<GameCard> result = new List<GameCard>();
            for (int i = 0; i < publicCards.Count; i++)
            {
                for (int j = 0; j < pool.Count; j++)
                {
                    if (pool[j].Color == publicCards[i].Color)
                        result.Add(pool[j]);
                }
            }
            return result.Count > 0 ? result : pool;
        }

        private static void RemoveKnownCard(List<GameCard> pool, GameCard knownCard)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].Color == knownCard.Color && pool[i].Number == knownCard.Number)
                {
                    pool.RemoveAt(i);
                    return;
                }
            }
        }

        private static float EstimateWinRate(GameCard enemyCard, List<GameCard> inferredPlayerCards, float drawValue)
        {
            if (inferredPlayerCards == null || inferredPlayerCards.Count == 0) return 0.5f;

            float score = 0f;
            for (int i = 0; i < inferredPlayerCards.Count; i++)
            {
                int result = ScoreSystem.Compare(inferredPlayerCards[i].Number, enemyCard.Number);
                if (result == ScoreSystem.EnemyWin) score += 1f;
                else if (result == ScoreSystem.Draw) score += drawValue;
            }
            return score / inferredPlayerCards.Count;
        }

        private static float EstimateFutureHandValueAfterPlay(GameCard candidate, List<GameCard> hand)
        {
            if (hand.Count <= 1) return 0f;

            float value = 0f;
            for (int i = 0; i < hand.Count; i++)
            {
                GameCard card = hand[i];
                if (card.Color == candidate.Color && card.Number == candidate.Number) continue;
                value += GetCardPower(card);
            }
            return value / (hand.Count - 1);
        }

        private static float EstimatePlayerThreat(List<GameCard> inferredPlayerCards)
        {
            if (inferredPlayerCards == null || inferredPlayerCards.Count == 0) return 0f;

            float threat = 0f;
            for (int i = 0; i < inferredPlayerCards.Count; i++)
                threat = Mathf.Max(threat, GetCardPower(inferredPlayerCards[i]));
            return threat;
        }

        private static float GetPreservationValue(GameCard candidate, List<GameCard> hand, EnemyAIDecisionContext context, float multiplier)
        {
            int remainingRoundsAfterThis = Mathf.Max(0, ScoreSystem.TotalRounds - context.CurrentRound);
            if (remainingRoundsAfterThis <= 0) return 0f;

            float highFutureScore = 0f;
            for (int roundIndex = context.CurrentRound; roundIndex < ScoreSystem.RoundScores.Length; roundIndex++)
                highFutureScore = Mathf.Max(highFutureScore, ScoreSystem.RoundScores[roundIndex]);

            float currentScore = GetCurrentRoundScore(context);
            float futurePressure = Mathf.Max(0f, highFutureScore - currentScore) / 10f;
            bool hasAlternativeStrongCard = false;
            for (int i = 0; i < hand.Count; i++)
            {
                GameCard other = hand[i];
                if (other.Color == candidate.Color && other.Number == candidate.Number) continue;
                if (GetCardPower(other) >= GetCardPower(candidate) - 0.5f)
                {
                    hasAlternativeStrongCard = true;
                    break;
                }
            }

            float preserve = (candidate.Number == 1 || candidate.Number == 5) ? 4f : 0f;
            preserve += Mathf.Max(0f, GetCardPower(candidate) - 3f) * futurePressure;
            if (hasAlternativeStrongCard) preserve *= 0.55f;
            return preserve * multiplier;
        }

        private static float GetCardPower(GameCard card)
        {
            return card.Number == 1 ? 5.4f : card.Number;
        }

        private static float GetCurrentRoundScore(EnemyAIDecisionContext context)
        {
            return ScoreSystem.GetRoundScore(context.CurrentRound - 1);
        }

        private static float GetScorePressure(EnemyAIDecisionContext context)
        {
            return Mathf.Clamp((context.PlayerScore - context.EnemyScore) / 10f, -3f, 3f);
        }

        private static void LogDecision(EnemyAIDecisionContext context, GameCard picked, float score, StringBuilder candidates, int inferredCount)
        {
            if (!ShouldLogAI()) return;

            string turn = context.EnemyActsSecond ? $"后手 对{GetColorLabel(context.PlayerPlayedPublicCard.Color)}?" : "先手";
            Debug.Log($"[AI:{GetDifficultyLabel(context.Difficulty)}] R{context.CurrentRound} {turn} 出{ShortCardName(picked)} score={score:0.0} inferred={inferredCount} candidates={candidates}");
        }

        private static string GetDifficultyLabel(EnemyAIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case EnemyAIDifficulty.God: return "神级";
                case EnemyAIDifficulty.Strong: return "强力";
                default: return "普通";
            }
        }

        private static string ShortCardName(GameCard card)
        {
            return GetColorLabel(card.Color) + card.Number;
        }

        private static string GetColorLabel(CardColor color)
        {
            switch (color)
            {
                case CardColor.Blue: return "蓝";
                case CardColor.Red: return "红";
                default: return "绿";
            }
        }

        private static bool ShouldLogAI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }
    }
}
