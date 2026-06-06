using System;
using System.Collections.Generic;
using System.Linq;

namespace Test1.BoardGame
{
    public static class Test1DealService
    {
        public static Test1DealResult Deal(int seed, Test1GameRuleConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            config.EnsureValidDefaults();
            System.Random random = new System.Random(seed);

            for (int retry = 0; retry < config.MaxDealRetryCount; retry++)
            {
                // Deal from a fresh deck each retry because the hand constraints may reject a layout.
                List<Test1Card> deck = CreateFullDeck(config);
                Shuffle(deck, random);

                Test1Card removedCard = deck[0];
                deck.RemoveAt(0);
                removedCard.Owner = Test1PlayerId.None;
                removedCard.Zone = Test1CardZone.Removed;
                removedCard.IsFaceUp = false;

                Dictionary<Test1CardColor, List<Test1Card>> groups = deck
                    .GroupBy(card => card.Color)
                    .ToDictionary(group => group.Key, group => group.ToList());

                List<Test1Card> playerAHand = new List<Test1Card>();
                List<Test1Card> playerBHand = new List<Test1Card>();

                DealRemovedColorGroup(groups[removedCard.Color], playerAHand, playerBHand, random);
                DealFullColorGroups(config, groups, removedCard.Color, playerAHand, playerBHand, random);
                MarkHand(playerAHand, Test1PlayerId.PlayerA);
                MarkHand(playerBHand, Test1PlayerId.PlayerB);

                if (IsValidDeal(playerAHand, playerBHand, config))
                {
                    SortHand(playerAHand);
                    SortHand(playerBHand);
                    List<Test1Card> allCards = new List<Test1Card>(deck);
                    allCards.Add(removedCard);
                    return new Test1DealResult(allCards, removedCard, playerAHand, playerBHand, retry);
                }
            }

            throw new InvalidOperationException("Deal failed after " + config.MaxDealRetryCount + " retries.");
        }

        private static List<Test1Card> CreateFullDeck(Test1GameRuleConfig config)
        {
            List<Test1Card> deck = new List<Test1Card>();
            foreach (Test1CardColor color in config.Colors)
            {
                foreach (int number in config.Numbers)
                {
                    deck.Add(new Test1Card(color, number));
                }
            }

            if (deck.Count != 15)
            {
                throw new InvalidOperationException("The current rules require exactly 15 cards: 3 colors x 5 numbers.");
            }

            return deck;
        }

        private static void DealRemovedColorGroup(
            List<Test1Card> cards,
            List<Test1Card> playerAHand,
            List<Test1Card> playerBHand,
            System.Random random)
        {
            // The removed color has four cards left, so each player receives exactly two.
            Shuffle(cards, random);
            playerAHand.AddRange(cards.Take(2));
            playerBHand.AddRange(cards.Skip(2).Take(2));
        }

        private static void DealFullColorGroups(
            Test1GameRuleConfig config,
            Dictionary<Test1CardColor, List<Test1Card>> groups,
            Test1CardColor removedColor,
            List<Test1Card> playerAHand,
            List<Test1Card> playerBHand,
            System.Random random)
        {
            // The two full colors split as 3/2 and 2/3 so every player ends with 2/2/3 colors.
            Test1CardColor[] fullColors = config.Colors.Where(color => color != removedColor).ToArray();
            bool playerAStartsWithThree = random.Next(0, 2) == 0;

            DealFiveCardColorGroup(groups[fullColors[0]], playerAStartsWithThree ? 3 : 2, playerAHand, playerBHand, random);
            DealFiveCardColorGroup(groups[fullColors[1]], playerAStartsWithThree ? 2 : 3, playerAHand, playerBHand, random);
        }

        private static void DealFiveCardColorGroup(
            List<Test1Card> cards,
            int playerACount,
            List<Test1Card> playerAHand,
            List<Test1Card> playerBHand,
            System.Random random)
        {
            Shuffle(cards, random);
            playerAHand.AddRange(cards.Take(playerACount));
            playerBHand.AddRange(cards.Skip(playerACount));
        }

        private static void MarkHand(List<Test1Card> hand, Test1PlayerId owner)
        {
            foreach (Test1Card card in hand)
            {
                card.Owner = owner;
                card.Zone = Test1CardZone.Hand;
                card.IsFaceUp = false;
                card.PlayedRoundIndex = 0;
            }
        }

        private static bool IsValidDeal(List<Test1Card> playerAHand, List<Test1Card> playerBHand, Test1GameRuleConfig config)
        {
            // The 1 and 5 constraint keeps the core 1-vs-5 mind game available to both players.
            if (playerAHand.Count != 7 || playerBHand.Count != 7)
            {
                return false;
            }

            if (!HasColorDistributionTwoTwoThree(playerAHand) || !HasColorDistributionTwoTwoThree(playerBHand))
            {
                return false;
            }

            if (!config.RequireEachPlayerHasOneAndFive)
            {
                return true;
            }

            return HasNumber(playerAHand, 1)
                && HasNumber(playerAHand, 5)
                && HasNumber(playerBHand, 1)
                && HasNumber(playerBHand, 5);
        }

        private static bool HasColorDistributionTwoTwoThree(List<Test1Card> hand)
        {
            int[] counts = hand
                .GroupBy(card => card.Color)
                .Select(group => group.Count())
                .OrderBy(count => count)
                .ToArray();

            return counts.Length == 3 && counts[0] == 2 && counts[1] == 2 && counts[2] == 3;
        }

        private static bool HasNumber(List<Test1Card> hand, int number)
        {
            return hand.Any(card => card.Number == number);
        }

        private static void SortHand(List<Test1Card> hand)
        {
            hand.Sort((left, right) =>
            {
                int colorCompare = left.Color.CompareTo(right.Color);
                return colorCompare != 0 ? colorCompare : left.Number.CompareTo(right.Number);
            });
        }

        private static void Shuffle<T>(IList<T> list, System.Random random)
        {
            // Fisher-Yates keeps dealing deterministic for the same seed.
            for (int index = list.Count - 1; index > 0; index--)
            {
                int swapIndex = random.Next(index + 1);
                T temp = list[index];
                list[index] = list[swapIndex];
                list[swapIndex] = temp;
            }
        }
    }
}
