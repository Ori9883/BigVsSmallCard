using UnityEngine;

namespace Test1.BoardGame
{
    [CreateAssetMenu(fileName = "Test1GameRuleConfig", menuName = "Test1/Board Game Rule Config")]
    public sealed class Test1GameRuleConfig : ScriptableObject
    {
        public int[] RoundScores = { 10, 10, 20, 20, 30, 30, 20 };
        public Test1CardColor[] Colors = { Test1CardColor.Red, Test1CardColor.Green, Test1CardColor.Blue };
        public int[] Numbers = { 1, 2, 3, 4, 5 };
        public int MaxDealRetryCount = 1000;
        public Test1TurnPolicy TurnPolicy = Test1TurnPolicy.RandomFirstThenAlternate;
        public bool EnablePeekSwap = true;
        public bool RequireEachPlayerHasOneAndFive = true;

        public int RoundCount
        {
            get { return RoundScores == null ? 0 : RoundScores.Length; }
        }

        public int GetRoundScore(int roundIndex)
        {
            if (RoundScores == null || roundIndex < 1 || roundIndex > RoundScores.Length)
            {
                return 0;
            }

            return RoundScores[roundIndex - 1];
        }

        public void EnsureValidDefaults()
        {
            if (RoundScores == null || RoundScores.Length != 7)
            {
                RoundScores = new[] { 10, 10, 20, 20, 30, 30, 20 };
            }

            if (Colors == null || Colors.Length != 3)
            {
                Colors = new[] { Test1CardColor.Red, Test1CardColor.Green, Test1CardColor.Blue };
            }

            if (Numbers == null || Numbers.Length != 5)
            {
                Numbers = new[] { 1, 2, 3, 4, 5 };
            }

            if (MaxDealRetryCount <= 0)
            {
                MaxDealRetryCount = 1000;
            }
        }

        public static Test1GameRuleConfig CreateRuntimeDefault()
        {
            Test1GameRuleConfig config = CreateInstance<Test1GameRuleConfig>();
            config.name = "Runtime Test1 Game Rule Config";
            config.EnsureValidDefaults();
            return config;
        }

        private void OnValidate()
        {
            EnsureValidDefaults();
        }
    }
}
