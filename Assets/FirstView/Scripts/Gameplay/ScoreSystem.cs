using UnityEngine;

namespace FirstView.Gameplay
{
    public static class ScoreSystem
    {
        public static readonly int[] RoundScores = { 10, 10, 20, 20, 30, 30, 20 };
        public const int TotalRounds = 7;

        public const int PlayerWin = 1;
        public const int EnemyWin = -1;
        public const int Draw = 0;

        public static int Compare(int playerNum, int enemyNum)
        {
            if (playerNum == enemyNum) return Draw;
            if (playerNum == 1 && enemyNum == 5) return PlayerWin;
            if (enemyNum == 1 && playerNum == 5) return EnemyWin;
            return playerNum > enemyNum ? PlayerWin : EnemyWin;
        }

        public static int GetRoundScore(int roundIndex)
        {
            if (roundIndex < 0 || roundIndex >= RoundScores.Length) return 0;
            return RoundScores[roundIndex];
        }
    }
}
