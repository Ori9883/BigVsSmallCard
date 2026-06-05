using System.Collections.Generic;
using System.Linq;

namespace BoardGame
{
    public static class RuleEngine
    {
        public static PlayerId Compare(CardData firstCard, CardData secondCard)
        {
            if (firstCard.Number == secondCard.Number)
            {
                return PlayerId.None;
            }

            bool isOneVsFive =
                (firstCard.Number == 1 && secondCard.Number == 5) ||
                (firstCard.Number == 5 && secondCard.Number == 1);

            if (isOneVsFive)
            {
                return firstCard.Number == 1 ? firstCard.Owner : secondCard.Owner;
            }

            return firstCard.Number > secondCard.Number ? firstCard.Owner : secondCard.Owner;
        }

        public static RoundResultType GetResultType(PlayerId winner)
        {
            if (winner == PlayerId.PlayerA)
            {
                return RoundResultType.PlayerAWin;
            }

            if (winner == PlayerId.PlayerB)
            {
                return RoundResultType.PlayerBWin;
            }

            return RoundResultType.Tie;
        }

        public static PlayerId ResolvePeekOwner(IReadOnlyList<RoundState> firstThreeRounds)
        {
            if (firstThreeRounds == null || firstThreeRounds.Count < 3)
            {
                return PlayerId.None;
            }

            int aWins = firstThreeRounds.Count(round => round.Winner == PlayerId.PlayerA);
            int bWins = firstThreeRounds.Count(round => round.Winner == PlayerId.PlayerB);

            if (aWins > bWins)
            {
                return PlayerId.PlayerA;
            }

            if (bWins > aWins)
            {
                return PlayerId.PlayerB;
            }

            PlayerId thirdRoundWinner = firstThreeRounds[2].Winner;
            if (aWins == 1 && bWins == 1 && thirdRoundWinner != PlayerId.None)
            {
                return thirdRoundWinner;
            }

            return PlayerId.None;
        }

        public static PlayerId ResolveGameWinner(PlayerState playerA, PlayerState playerB)
        {
            if (playerA.Score == playerB.Score)
            {
                return PlayerId.None;
            }

            return playerA.Score > playerB.Score ? PlayerId.PlayerA : PlayerId.PlayerB;
        }
    }
}
