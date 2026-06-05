using System;
using System.Collections.Generic;
using System.Linq;

namespace BoardGame
{
    public static class DealService
    {
        public static DealResult Deal(int seed, GameRuleConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            Random random = new Random(seed);

            for (int retry = 0; retry < config.MaxDealRetryCount; retry++)
            {
                List<CardData> allCards = CreateAllCards(config);
                CardData removedCard = PickAndRemoveRandomCard(allCards, random);
                Dictionary<CardColor, List<CardData>> groups = GroupByColor(allCards);

                List<CardData> playerAHand = new List<CardData>();
                List<CardData> playerBHand = new List<CardData>();

                DealRemovedColorGroup(groups[removedCard.Color], playerAHand, playerBHand, random);
                DealTwoFullColorGroups(groups, removedCard.Color, playerAHand, playerBHand, random);

                if (IsValidHand(playerAHand, config) && IsValidHand(playerBHand, config))
                {
                    AssignHand(playerAHand, PlayerId.PlayerA);
                    AssignHand(playerBHand, PlayerId.PlayerB);
                    SortHand(playerAHand);
                    SortHand(playerBHand);
                    return new DealResult(allCards, removedCard, playerAHand, playerBHand, retry);
                }
            }

            throw new InvalidOperationException("Deal failed after max retry count.");
        }

        public static List<CardData> CreateAllCards(GameRuleConfig config)
        {
            List<CardData> cards = new List<CardData>();
            for (int c = 0; c < config.Colors.Length; c++)
            {
                CardColor color = config.Colors[c];
                for (int n = 0; n < config.Numbers.Length; n++)
                {
                    int number = config.Numbers[n];
                    cards.Add(new CardData(color + "_" + number, color, number));
                }
            }

            return cards;
        }

        public static bool IsValidHand(List<CardData> hand, GameRuleConfig config)
        {
            if (hand == null || hand.Count != 7)
            {
                return false;
            }

            if (config.RequireEachPlayerHasOneAndFive)
            {
                bool hasOne = hand.Any(card => card.Number == 1);
                bool hasFive = hand.Any(card => card.Number == 5);
                if (!hasOne || !hasFive)
                {
                    return false;
                }
            }

            IEnumerable<int> colorCounts = hand.GroupBy(card => card.Color).Select(group => group.Count()).OrderBy(count => count);
            return string.Join(",", colorCounts) == "2,2,3";
        }

        private static CardData PickAndRemoveRandomCard(List<CardData> allCards, Random random)
        {
            int index = random.Next(allCards.Count);
            CardData removedCard = allCards[index];
            allCards.RemoveAt(index);
            removedCard.Owner = PlayerId.None;
            removedCard.Zone = CardZone.Removed;
            removedCard.IsFaceUp = false;
            return removedCard;
        }

        private static Dictionary<CardColor, List<CardData>> GroupByColor(List<CardData> cards)
        {
            return cards.GroupBy(card => card.Color).ToDictionary(group => group.Key, group => group.ToList());
        }

        private static void DealRemovedColorGroup(List<CardData> group, List<CardData> playerAHand, List<CardData> playerBHand, Random random)
        {
            Shuffle(group, random);
            playerAHand.Add(group[0]);
            playerAHand.Add(group[1]);
            playerBHand.Add(group[2]);
            playerBHand.Add(group[3]);
        }

        private static void DealTwoFullColorGroups(Dictionary<CardColor, List<CardData>> groups, CardColor removedColor, List<CardData> playerAHand, List<CardData> playerBHand, Random random)
        {
            List<CardColor> fullColors = groups.Keys.Where(color => color != removedColor).ToList();
            if (fullColors.Count != 2)
            {
                throw new InvalidOperationException("Expected two full color groups after removing one card.");
            }

            Shuffle(groups[fullColors[0]], random);
            Shuffle(groups[fullColors[1]], random);

            bool playerATakesThreeFromFirst = random.Next(2) == 0;
            DealFullColorGroup(groups[fullColors[0]], playerAHand, playerBHand, playerATakesThreeFromFirst ? 3 : 2);
            DealFullColorGroup(groups[fullColors[1]], playerAHand, playerBHand, playerATakesThreeFromFirst ? 2 : 3);
        }

        private static void DealFullColorGroup(List<CardData> group, List<CardData> playerAHand, List<CardData> playerBHand, int playerACount)
        {
            for (int i = 0; i < group.Count; i++)
            {
                if (i < playerACount)
                {
                    playerAHand.Add(group[i]);
                }
                else
                {
                    playerBHand.Add(group[i]);
                }
            }
        }

        private static void AssignHand(List<CardData> hand, PlayerId owner)
        {
            for (int i = 0; i < hand.Count; i++)
            {
                hand[i].Owner = owner;
                hand[i].Zone = CardZone.Hand;
                hand[i].IsFaceUp = false;
            }
        }

        public static void SortHand(List<CardData> hand)
        {
            hand.Sort((left, right) =>
            {
                int colorCompare = left.Color.CompareTo(right.Color);
                return colorCompare != 0 ? colorCompare : left.Number.CompareTo(right.Number);
            });
        }

        private static void Shuffle<T>(IList<T> items, Random random)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temp = items[i];
                items[i] = items[j];
                items[j] = temp;
            }
        }
    }
}
