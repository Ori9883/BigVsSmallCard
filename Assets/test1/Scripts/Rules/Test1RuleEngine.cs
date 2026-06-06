using System;
using System.Collections.Generic;
using System.Linq;

namespace Test1.BoardGame
{
    public static class Test1RuleEngine
    {
        public static Test1RoundResultType CompareCards(Test1Card firstCard, Test1Card secondCard, out Test1PlayerId winner)
        {
            // Color is intentionally ignored here; it is public information but not battle strength.
            if (firstCard == null)
            {
                throw new ArgumentNullException(nameof(firstCard));
            }

            if (secondCard == null)
            {
                throw new ArgumentNullException(nameof(secondCard));
            }

            if (firstCard.Number == secondCard.Number)
            {
                winner = Test1PlayerId.None;
                return Test1RoundResultType.Tie;
            }

            bool oneAgainstFive =
                (firstCard.Number == 1 && secondCard.Number == 5) ||
                (firstCard.Number == 5 && secondCard.Number == 1);

            // Special rule: 1 only beats 5 when they directly meet.
            if (oneAgainstFive)
            {
                winner = firstCard.Number == 1 ? firstCard.Owner : secondCard.Owner;
                return GetRoundResultType(winner);
            }

            winner = firstCard.Number > secondCard.Number ? firstCard.Owner : secondCard.Owner;
            return GetRoundResultType(winner);
        }

        public static void ApplyRoundResult(Test1RoundState round, Test1PlayerState playerA, Test1PlayerState playerB)
        {
            Test1PlayerId winner;
            round.ResultType = CompareCards(round.FirstPlayedCard, round.SecondPlayedCard, out winner);
            round.Winner = winner;

            if (winner == Test1PlayerId.PlayerA)
            {
                playerA.Score += round.RoundScore;
            }
            else if (winner == Test1PlayerId.PlayerB)
            {
                playerB.Score += round.RoundScore;
            }

            if (round.RoundIndex <= 3 && winner == Test1PlayerId.PlayerA)
            {
                playerA.FirstThreeRoundWinCount++;
            }
            else if (round.RoundIndex <= 3 && winner == Test1PlayerId.PlayerB)
            {
                playerB.FirstThreeRoundWinCount++;
            }

            round.ScoreAfterRoundA = playerA.Score;
            round.ScoreAfterRoundB = playerB.Score;
        }

        public static Test1PlayerId ResolvePeekOwner(IReadOnlyList<Test1RoundState> roundHistory)
        {
            // Tied rounds do not count; if first-three wins tie 1:1, the third-round winner breaks it.
            if (roundHistory == null || roundHistory.Count < 3)
            {
                return Test1PlayerId.None;
            }

            List<Test1RoundState> firstThreeRounds = roundHistory.Take(3).ToList();
            int playerAWins = firstThreeRounds.Count(round => round.Winner == Test1PlayerId.PlayerA);
            int playerBWins = firstThreeRounds.Count(round => round.Winner == Test1PlayerId.PlayerB);

            if (playerAWins > playerBWins)
            {
                return Test1PlayerId.PlayerA;
            }

            if (playerBWins > playerAWins)
            {
                return Test1PlayerId.PlayerB;
            }

            Test1PlayerId thirdRoundWinner = firstThreeRounds[2].Winner;
            if (playerAWins == 1 && playerBWins == 1 && thirdRoundWinner != Test1PlayerId.None)
            {
                return thirdRoundWinner;
            }

            return Test1PlayerId.None;
        }

        public static Test1GameResultType ResolveGameResult(Test1PlayerState playerA, Test1PlayerState playerB, out Test1PlayerId winner)
        {
            if (playerA.Score > playerB.Score)
            {
                winner = Test1PlayerId.PlayerA;
                return Test1GameResultType.PlayerAWin;
            }

            if (playerB.Score > playerA.Score)
            {
                winner = Test1PlayerId.PlayerB;
                return Test1GameResultType.PlayerBWin;
            }

            winner = Test1PlayerId.None;
            return Test1GameResultType.Tie;
        }

        private static Test1RoundResultType GetRoundResultType(Test1PlayerId winner)
        {
            switch (winner)
            {
                case Test1PlayerId.PlayerA:
                    return Test1RoundResultType.PlayerAWin;
                case Test1PlayerId.PlayerB:
                    return Test1RoundResultType.PlayerBWin;
                default:
                    return Test1RoundResultType.Tie;
            }
        }
    }
}
