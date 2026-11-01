namespace Game.Core
{
    /// <summary>
    /// Points. In the mine: a group of n blocks gives n·n·5, so big groups pay much more than small ones;
    /// every chest that falls out gives 100 per star. On the building site: a part gives 10 per tier,
    /// a finished building gives 500 times its number.
    /// </summary>
    public static class Scoring
    {
        public static int Pop(int size) => size <= 0 ? 0 : size * size * 5;
        public static int Chest(Chest chest) => 100 * System.Math.Max(1, chest.Stars);
        public static int Place(int tier) => 10 * System.Math.Max(1, tier);
        public static int Building(int castleIndex) => 500 * (castleIndex + 1);
    }
}
