using System;

namespace BoardGame
{
    public sealed class TurnManager
    {
        private readonly TurnPolicy turnPolicy;
        private readonly Random random;
        private PlayerId firstRoundPlayer = PlayerId.None;

        public TurnManager(TurnPolicy turnPolicy, Random random)
        {
            this.turnPolicy = turnPolicy;
            this.random = random;
        }

        public PlayerId GetFirstPlayer(int roundIndex, PlayerId previousFirstPlayer, PlayerId previousWinner)
        {
            switch (turnPolicy)
            {
                case TurnPolicy.FixedPlayerAFirst:
                    return PlayerId.PlayerA;
                case TurnPolicy.FixedPlayerBFirst:
                    return PlayerId.PlayerB;
                case TurnPolicy.RandomFirstThenWinnerLeads:
                    if (roundIndex == 1)
                    {
                        firstRoundPlayer = RandomPlayer();
                        return firstRoundPlayer;
                    }

                    return previousWinner == PlayerId.None ? OpponentOf(previousFirstPlayer) : previousWinner;
                case TurnPolicy.RandomFirstThenAlternate:
                default:
                    if (roundIndex == 1)
                    {
                        firstRoundPlayer = RandomPlayer();
                        return firstRoundPlayer;
                    }

                    return OpponentOf(previousFirstPlayer);
            }
        }

        public static PlayerId OpponentOf(PlayerId playerId)
        {
            return playerId == PlayerId.PlayerA ? PlayerId.PlayerB : PlayerId.PlayerA;
        }

        private PlayerId RandomPlayer()
        {
            return random.Next(2) == 0 ? PlayerId.PlayerA : PlayerId.PlayerB;
        }
    }
}
