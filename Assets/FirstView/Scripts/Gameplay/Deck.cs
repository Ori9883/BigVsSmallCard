using System.Collections.Generic;
using UnityEngine;

namespace FirstView.Gameplay
{
    public static class Deck
    {
        public const int CardsPerColor = 5;
        public const int HandSize = 7;

        public static List<GameCard> CreateFullDeck()
        {
            var deck = new List<GameCard>(15);
            var colors = new CardColor[] { CardColor.Green, CardColor.Blue, CardColor.Red };
            foreach (var c in colors)
                for (int n = 1; n <= CardsPerColor; n++)
                    deck.Add(new GameCard(c, n));
            return deck;
        }

        public static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        public static bool Deal(out List<GameCard> playerHand, out List<GameCard> enemyHand, out GameCard removed)
        {
            for (int attempt = 0; attempt < 100; attempt++)
            {
                var pool = CreateFullDeck();
                Shuffle(pool);

                removed = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);

                int threeColor = Random.Range(0, 3);
                int[] quotas = new int[3] { 2, 2, 2 };
                quotas[threeColor] = 3;

                var pHand = new List<GameCard>(HandSize);
                var remaining = new List<GameCard>(pool);

                if (!TryBuildHand(remaining, pHand, quotas))
                    continue;

                var eHand = new List<GameCard>(remaining);
                if (!HasOneAndFive(eHand))
                    continue;

                playerHand = pHand;
                enemyHand = eHand;
                return true;
            }

            playerHand = null;
            enemyHand = null;
            removed = default;
            Debug.LogError("[Deck] Failed to deal after 100 attempts");
            return false;
        }

        private static bool TryBuildHand(List<GameCard> pool, List<GameCard> hand, int[] quotas)
        {
            var colors = new CardColor[] { CardColor.Green, CardColor.Blue, CardColor.Red };

            bool hasOne = false, hasFive = false;

            for (int ci = 0; ci < 3; ci++)
            {
                int needed = quotas[ci];
                var candidates = pool.FindAll(c => c.Color == colors[ci]);
                if (candidates.Count < needed)
                    return false;

                bool pickedOne = false, pickedFive = false;

                if (!hasOne)
                {
                    int idx = candidates.FindIndex(c => c.Number == 1);
                    if (idx >= 0)
                    {
                        hand.Add(candidates[idx]);
                        candidates.RemoveAt(idx);
                        hasOne = true;
                        pickedOne = true;
                        needed--;
                    }
                }

                if (!hasFive && needed > 0)
                {
                    int idx = candidates.FindIndex(c => c.Number == 5);
                    if (idx >= 0)
                    {
                        hand.Add(candidates[idx]);
                        candidates.RemoveAt(idx);
                        hasFive = true;
                        pickedFive = true;
                        needed--;
                    }
                }

                for (int i = candidates.Count - 1; needed > 0 && i >= 0; i--)
                {
                    if (!pickedOne || candidates[i].Number != 1)
                    {
                        if (!pickedFive || candidates[i].Number != 5)
                        {
                            hand.Add(candidates[i]);
                            candidates.RemoveAt(i);
                            needed--;
                        }
                    }
                }

                if (needed > 0)
                    return false;
            }

            if (!hasOne || !hasFive)
                return false;

            pool.RemoveAll(c => hand.Contains(c));
            return true;
        }

        private static bool HasOneAndFive(List<GameCard> cards)
        {
            bool hasOne = false, hasFive = false;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].Number == 1) hasOne = true;
                if (cards[i].Number == 5) hasFive = true;
            }
            return hasOne && hasFive;
        }
    }
}
