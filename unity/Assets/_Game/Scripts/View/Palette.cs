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
        public static readonly Color32 BoardBack = new Color32(0x8a, 0x6a, 0x4c, 0xff);

        public static Color Block(int color) => Blocks[Mathf.Clamp(color, 0, Blocks.Length - 1)];
    }
}
