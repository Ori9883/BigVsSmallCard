using System.Collections.Generic;
using UnityEngine;

namespace FirstView.Gameplay
{
    public class GameSession : MonoBehaviour
    {
        public List<GameCard> PlayerHand { get; private set; }
        public List<GameCard> EnemyHand { get; private set; }
        public GameCard RemovedCard { get; private set; }
        public bool IsDealt { get; private set; }

        public void Deal()
        {
            if (IsDealt) return;

            if (!Deck.Deal(out var playerHand, out var enemyHand, out var removed))
            {
                Debug.LogError("[GameSession] Deal failed");
                return;
            }

            PlayerHand = playerHand;
            EnemyHand = enemyHand;
            RemovedCard = removed;
            IsDealt = true;

            LogHands();
        }

        private void LogHands()
        {
            string p = "Player: ";
            for (int i = 0; i < PlayerHand.Count; i++) p += PlayerHand[i] + " ";
            string e = "Enemy: ";
            for (int i = 0; i < EnemyHand.Count; i++) e += EnemyHand[i] + " ";
            Debug.Log($"[GameSession] Removed={RemovedCard}\n{p}\n{e}");
        }
    }
}
