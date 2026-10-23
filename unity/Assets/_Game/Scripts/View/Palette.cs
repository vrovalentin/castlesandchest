using UnityEngine;

namespace Game.View
{
    /// <summary>Colours from the prototype (prototype/style.css). Placeholder until the real art.</summary>
    public static class Palette
    {
        /// <summary>Block and chest colour by colour index: green, brown, gray, blue, purple.</summary>
        public static readonly Color32[] Blocks =
        {
            new Color32(0x4f, 0xb3, 0x5a, 0xff),
            new Color32(0xa8, 0x70, 0x3f, 0xff),
            new Color32(0x98, 0xa2, 0xad, 0xff),
            new Color32(0x4f, 0x8f, 0xe0, 0xff),
            new Color32(0x8e, 0x5b, 0xd6, 0xff),
        };

        public static readonly string[] BlockNames = { "зелёный", "коричневый", "серый", "синий", "фиолетовый" };

        public static readonly Color32 Background = new Color32(0xf3, 0xea, 0xd8, 0xff);
        /// <summary>The mine is dark so the blocks stand out: dark brown tray on an even darker screen.</summary>
        public static readonly Color32 BoardBack = new Color32(0x4a, 0x38, 0x29, 0xff);
        public static readonly Color32 MineBackground = new Color32(0x2e, 0x23, 0x1a, 0xff);
        public static readonly Color32 MineText = new Color32(0xf0, 0xe6, 0xd6, 0xff);

        /// <summary>Colour of a part tier 1..7 in lists (index 0 unused).</summary>
        public static readonly Color32[] Tiers =
        {
            new Color32(0x80, 0x80, 0x80, 0xff),
            new Color32(0x4f, 0xb3, 0x5a, 0xff),
            new Color32(0xa8, 0x70, 0x3f, 0xff),
            new Color32(0x98, 0xa2, 0xad, 0xff),
            new Color32(0x4f, 0x8f, 0xe0, 0xff),
            new Color32(0x8e, 0x5b, 0xd6, 0xff),
            new Color32(0xd3, 0x9a, 0x12, 0xff),
            new Color32(0xe0, 0x6a, 0xa6, 0xff),
        };

        public static readonly string[] TierNames = { "", "Глина", "Кирпичи", "Стенки", "Перекрытия и кровля", "Окна и двери", "Декор", "Фигурки" };

        public static Color Tier(int tier) => Tiers[Mathf.Clamp(tier, 0, Tiers.Length - 1)];

        public static Color Block(int color) => Blocks[Mathf.Clamp(color, 0, Blocks.Length - 1)];
    }
}
