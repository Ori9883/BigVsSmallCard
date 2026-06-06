using System;
using System.Collections.Generic;
using System.Linq;

namespace Test1.BoardGame
{
    public enum Test1CardColor
    {
        Red,
        Green,
        Blue
    }

    public enum Test1PlayerId
    {
        None,
        PlayerA,
        PlayerB
    }

    public enum Test1CardZone
    {
        Deck,
        Removed,
        Hand,
        Played,
        Discard
    }

    public enum Test1RoundResultType
    {
        Tie,
        PlayerAWin,
        PlayerBWin
    }

    public enum Test1GameResultType
    {
        None,
        PlayerAWin,
        PlayerBWin,
        Tie
    }

    public enum Test1GamePhase
    {
        NotStarted,
        Dealing,
        WaitingForFirstPlayer,
        WaitingForSecondPlayer,
        Reveal,
        RoundSettlement,
        PeekDecision,
        WaitingForSwapCard,
        GameOver
    }

    public enum Test1TurnPolicy
    {
        RandomFirstThenAlternate,
        RandomFirstThenWinnerLeads,
        FixedPlayerAFirst,
        FixedPlayerBFirst
    }

    public enum Test1CardVisibility
    {
        FaceUp,
        FaceDownColorOnly,
        Hidden
    }

    [Serializable]
    public sealed class Test1Card
    {
        public string CardId;
        public Test1CardColor Color;
        public int Number;
        public Test1PlayerId Owner;
        public Test1CardZone Zone;
        public bool IsFaceUp;
        public int PlayedRoundIndex;

        public Test1Card()
        {
        }

        public Test1Card(Test1CardColor color, int number)
        {
            Color = color;
            Number = number;
            CardId = color + "_" + number;
            Owner = Test1PlayerId.None;
            Zone = Test1CardZone.Deck;
            IsFaceUp = false;
            PlayedRoundIndex = 0;
        }

        public string GetPublicName(bool showNumber)
        {
            return showNumber ? Color + " " + Number : Color + " ?";
        }

        public override string ToString()
        {
            return Color + "-" + Number;
        }
    }

    [Serializable]
    public sealed class Test1PlayerState
    {
        public Test1PlayerId PlayerId;
        public string DisplayName;
        public List<Test1Card> HandCards = new List<Test1Card>();
        public List<Test1Card> DiscardCards = new List<Test1Card>();
        public int Score;
        public int FirstThreeRoundWinCount;

        public Test1PlayerState()
        {
        }

        public Test1PlayerState(Test1PlayerId playerId, string displayName)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            Score = 0;
            FirstThreeRoundWinCount = 0;
        }

        public bool HasNumber(int number)
        {
            return HandCards.Any(card => card.Number == number);
        }

        public int CountColor(Test1CardColor color)
        {
            return HandCards.Count(card => card.Color == color);
        }
    }

    [Serializable]
    public sealed class Test1RoundState
    {
        public int RoundIndex;
        public int RoundScore;
        public Test1PlayerId FirstPlayer;
        public Test1PlayerId SecondPlayer;
        public Test1Card FirstPlayedCard;
        public Test1Card SecondPlayedCard;
        public Test1RoundResultType ResultType;
        public Test1PlayerId Winner;
        public int ScoreAfterRoundA;
        public int ScoreAfterRoundB;

        public Test1Card GetPlayedCard(Test1PlayerId playerId)
        {
            if (FirstPlayer == playerId)
            {
                return FirstPlayedCard;
            }

            if (SecondPlayer == playerId)
            {
                return SecondPlayedCard;
            }

            return null;
        }
    }

    [Serializable]
    public sealed class Test1GameState
    {
        public int RandomSeed;
        public int DealRetryCount;
        public Test1GamePhase Phase = Test1GamePhase.NotStarted;
        public int CurrentRoundIndex;
        public Test1PlayerId CurrentActor;
        public Test1PlayerState PlayerA = new Test1PlayerState(Test1PlayerId.PlayerA, "Player A");
        public Test1PlayerState PlayerB = new Test1PlayerState(Test1PlayerId.PlayerB, "Player B");
        public Test1Card RemovedCard;
        public List<Test1Card> AllCards = new List<Test1Card>();
        public List<Test1RoundState> RoundHistory = new List<Test1RoundState>();
        public Test1RoundState CurrentRound;
        public Test1PlayerId PeekOwner;
        public bool HasPeekResolved;
        public bool SwapOccurred;
        public Test1PlayerId SwapPlayer;
        public string SwappedOutCardId;
        public string SwappedInCardId;
        public Test1PlayerId LastRoundFirstPlayer;
        public Test1PlayerId LastRoundWinner;
        public Test1GameResultType GameResult;
        public Test1PlayerId FinalWinner;

        public Test1PlayerState GetPlayer(Test1PlayerId playerId)
        {
            switch (playerId)
            {
                case Test1PlayerId.PlayerA:
                    return PlayerA;
                case Test1PlayerId.PlayerB:
                    return PlayerB;
                default:
                    return null;
            }
        }

        public static Test1PlayerId OpponentOf(Test1PlayerId playerId)
        {
            switch (playerId)
            {
                case Test1PlayerId.PlayerA:
                    return Test1PlayerId.PlayerB;
                case Test1PlayerId.PlayerB:
                    return Test1PlayerId.PlayerA;
                default:
                    return Test1PlayerId.None;
            }
        }
    }

    public sealed class Test1DealResult
    {
        public List<Test1Card> AllCards;
        public Test1Card RemovedCard;
        public List<Test1Card> PlayerAHand;
        public List<Test1Card> PlayerBHand;
        public int RetryCount;

        public Test1DealResult(
            List<Test1Card> allCards,
            Test1Card removedCard,
            List<Test1Card> playerAHand,
            List<Test1Card> playerBHand,
            int retryCount)
        {
            AllCards = allCards;
            RemovedCard = removedCard;
            PlayerAHand = playerAHand;
            PlayerBHand = playerBHand;
            RetryCount = retryCount;
        }
    }
}
