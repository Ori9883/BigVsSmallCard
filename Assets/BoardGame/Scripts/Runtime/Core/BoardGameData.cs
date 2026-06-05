using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoardGame
{
    public enum CardColor
    {
        Red,
        Green,
        Blue
    }

    public enum PlayerId
    {
        None,
        PlayerA,
        PlayerB
    }

    public enum CardZone
    {
        Deck,
        Removed,
        Hand,
        Played,
        Discard
    }

    public enum RoundResultType
    {
        PlayerAWin,
        PlayerBWin,
        Tie
    }

    public enum GamePhase
    {
        Boot,
        MainMenu,
        Dealing,
        RoundStart,
        FirstPlayerSelecting,
        SecondPlayerSelecting,
        Reveal,
        RoundSettlement,
        PeekAndSwap,
        GameSettlement,
        Finished
    }

    public enum TurnPolicy
    {
        RandomFirstThenAlternate,
        RandomFirstThenWinnerLeads,
        FixedPlayerAFirst,
        FixedPlayerBFirst
    }

    [CreateAssetMenu(fileName = "GameRuleConfig", menuName = "BoardGame/Game Rule Config")]
    public sealed class GameRuleConfig : ScriptableObject
    {
        public int[] RoundScores = { 10, 10, 20, 20, 30, 30, 20 };
        public CardColor[] Colors = { CardColor.Red, CardColor.Green, CardColor.Blue };
        public int[] Numbers = { 1, 2, 3, 4, 5 };
        public int MaxDealRetryCount = 1000;
        public TurnPolicy TurnPolicy = TurnPolicy.RandomFirstThenAlternate;
        public bool EnablePeekSwap = true;
        public bool RequireEachPlayerHasOneAndFive = true;

        public static GameRuleConfig CreateRuntimeDefault()
        {
            GameRuleConfig config = CreateInstance<GameRuleConfig>();
            config.name = "Runtime Game Rule Config";
            return config;
        }
    }

    [Serializable]
    public sealed class CardData
    {
        public string CardId;
        public CardColor Color;
        public int Number;
        public PlayerId Owner;
        public CardZone Zone;
        public bool IsFaceUp;
        public int PlayedRoundIndex;

        public CardData(string cardId, CardColor color, int number)
        {
            CardId = cardId;
            Color = color;
            Number = number;
            Owner = PlayerId.None;
            Zone = CardZone.Deck;
            IsFaceUp = false;
            PlayedRoundIndex = 0;
        }
    }

    [Serializable]
    public sealed class PlayerState
    {
        public PlayerId PlayerId;
        public string DisplayName;
        public List<CardData> HandCards = new List<CardData>();
        public List<CardData> DiscardCards = new List<CardData>();
        public int Score;
        public int FirstThreeRoundWinCount;

        public PlayerState(PlayerId playerId, string displayName)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            Score = 0;
            FirstThreeRoundWinCount = 0;
        }
    }

    [Serializable]
    public sealed class RoundState
    {
        public int RoundIndex;
        public int RoundScore;
        public PlayerId FirstPlayer;
        public PlayerId SecondPlayer;
        public CardData FirstPlayedCard;
        public CardData SecondPlayedCard;
        public RoundResultType ResultType;
        public PlayerId Winner;
        public int ScoreAfterRoundA;
        public int ScoreAfterRoundB;
    }

    [Serializable]
    public sealed class GameSessionState
    {
        public int RandomSeed;
        public GamePhase Phase;
        public int CurrentRoundIndex;
        public PlayerState PlayerA;
        public PlayerState PlayerB;
        public CardData RemovedCard;
        public List<CardData> AllCards = new List<CardData>();
        public List<RoundState> RoundHistory = new List<RoundState>();
        public PlayerId PeekOwner;
        public bool HasPeekResolved;
        public bool SwapOccurred;
        public PlayerId SwapPlayer;
        public string SwappedOutCardId;
        public string SwappedInCardId;
        public PlayerId LastRoundFirstPlayer;
        public PlayerId LastRoundWinner;
    }

    public sealed class DealResult
    {
        public List<CardData> AllCards;
        public CardData RemovedCard;
        public List<CardData> PlayerAHand;
        public List<CardData> PlayerBHand;
        public int RetryCount;

        public DealResult(List<CardData> allCards, CardData removedCard, List<CardData> playerAHand, List<CardData> playerBHand, int retryCount)
        {
            AllCards = allCards;
            RemovedCard = removedCard;
            PlayerAHand = playerAHand;
            PlayerBHand = playerBHand;
            RetryCount = retryCount;
        }
    }
}
