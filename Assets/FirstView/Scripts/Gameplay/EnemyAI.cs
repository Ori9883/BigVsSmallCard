using System.Collections.Generic;
using UnityEngine;

namespace FirstView.Gameplay
{
    public static class EnemyAI
    {
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
    }
}
