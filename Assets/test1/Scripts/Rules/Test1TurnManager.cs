using System;

namespace Test1.BoardGame
{
    public sealed class Test1TurnManager
    {
        private readonly Test1TurnPolicy turnPolicy;
        private readonly System.Random random;

        public Test1TurnManager(Test1TurnPolicy turnPolicy, System.Random random)
        {
            this.turnPolicy = turnPolicy;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public Test1PlayerId GetFirstPlayer(int roundIndex, Test1PlayerId lastFirstPlayer, Test1PlayerId lastRoundWinner)
        {
            switch (turnPolicy)
            {
                case Test1TurnPolicy.FixedPlayerAFirst:
                    return Test1PlayerId.PlayerA;
                case Test1TurnPolicy.FixedPlayerBFirst:
                    return Test1PlayerId.PlayerB;
                case Test1TurnPolicy.RandomFirstThenWinnerLeads:
                    return GetWinnerLeadFirstPlayer(roundIndex, lastFirstPlayer, lastRoundWinner);
                case Test1TurnPolicy.RandomFirstThenAlternate:
                default:
                    return GetAlternateFirstPlayer(roundIndex, lastFirstPlayer);
            }
        }

        private Test1PlayerId GetAlternateFirstPlayer(int roundIndex, Test1PlayerId lastFirstPlayer)
        {
            // MVP default: random first round, then alternate to reduce continuous first-player bias.
            if (roundIndex <= 1 || lastFirstPlayer == Test1PlayerId.None)
            {
                return GetRandomPlayer();
            }

            return Test1GameState.OpponentOf(lastFirstPlayer);
        }

        private Test1PlayerId GetWinnerLeadFirstPlayer(int roundIndex, Test1PlayerId lastFirstPlayer, Test1PlayerId lastRoundWinner)
        {
            if (roundIndex <= 1 || lastFirstPlayer == Test1PlayerId.None)
            {
                return GetRandomPlayer();
            }

            // A tied round has no winner, so we alternate to avoid permanent first-player advantage.
            return lastRoundWinner == Test1PlayerId.None
                ? Test1GameState.OpponentOf(lastFirstPlayer)
                : lastRoundWinner;
        }

        private Test1PlayerId GetRandomPlayer()
        {
            return random.Next(0, 2) == 0 ? Test1PlayerId.PlayerA : Test1PlayerId.PlayerB;
        }
    }
}
