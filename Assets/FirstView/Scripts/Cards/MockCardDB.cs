using UnityEngine;

namespace FirstView
{
    /// <summary>
    /// ScriptableObject holding mock card definitions for the prototype.
    /// </summary>
    [CreateAssetMenu(fileName = "MockCardDB", menuName = "FirstView/Mock Card DB")]
    public class MockCardDB : ScriptableObject
    {
        public CardEntry[] cards;

        [System.Serializable]
        public class CardEntry
        {
            public string id;
            public string displayName;
            public int cost;
            public int attack;
            public int health;
            public string ability;
            public CardRarity rarity;
            public Color themeColor = Color.white;
        }

        public static MockCardDB CreateDefault()
        {
            var db = CreateInstance<MockCardDB>();
            db.cards = new CardEntry[]
            {
                new CardEntry
                {
                    id = "squirrel", displayName = "松鼠", cost = 0, attack = 0, health = 1,
                    ability = "祭品专用", rarity = CardRarity.Common,
                    themeColor = new Color(0.55f, 0.4f, 0.25f)
                },
                new CardEntry
                {
                    id = "wolf", displayName = "灰狼", cost = 2, attack = 3, health = 4,
                    ability = "无", rarity = CardRarity.Common,
                    themeColor = new Color(0.5f, 0.5f, 0.55f)
                },
                new CardEntry
                {
                    id = "raven", displayName = "渡鸦", cost = 1, attack = 2, health = 1,
                    ability = "飞行：绕过对位", rarity = CardRarity.Rare,
                    themeColor = new Color(0.2f, 0.2f, 0.35f)
                },
                new CardEntry
                {
                    id = "adder", displayName = "蝰蛇", cost = 1, attack = 1, health = 2,
                    ability = "毒性：每回合-1HP", rarity = CardRarity.Uncommon,
                    themeColor = new Color(0.2f, 0.5f, 0.2f)
                },
                new CardEntry
                {
                    id = "bear", displayName = "灰熊", cost = 3, attack = 4, health = 6,
                    ability = "无", rarity = CardRarity.Rare,
                    themeColor = new Color(0.45f, 0.3f, 0.2f)
                },
                new CardEntry
                {
                    id = "moth", displayName = "死神蛾", cost = 2, attack = 1, health = 2,
                    ability = "入场：抽1张牌", rarity = CardRarity.Uncommon,
                    themeColor = new Color(0.35f, 0.25f, 0.4f)
                },
                new CardEntry
                {
                    id = "cat", displayName = "黑猫", cost = 1, attack = 2, health = 2,
                    ability = "死亡时：抽1牌", rarity = CardRarity.Common,
                    themeColor = new Color(0.15f, 0.15f, 0.2f)
                },
                new CardEntry
                {
                    id = "stag", displayName = "雄鹿", cost = 2, attack = 2, health = 3,
                    ability = "入场：+1攻击力", rarity = CardRarity.Uncommon,
                    themeColor = new Color(0.5f, 0.4f, 0.25f)
                }
            };
            return db;
        }
    }
}
