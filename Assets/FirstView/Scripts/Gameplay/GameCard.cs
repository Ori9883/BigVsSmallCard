namespace FirstView.Gameplay
{
    public struct GameCard
    {
        public readonly CardColor Color;
        public readonly int Number;

        public GameCard(CardColor color, int number)
        {
            Color = color;
            Number = number;
        }

        public override string ToString() => $"{Color}{Number}";
    }
}
