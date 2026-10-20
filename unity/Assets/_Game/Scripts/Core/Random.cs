namespace Game.Core
{
    /// <summary>Source of random numbers for the game rules. Tests and the balance bot pass a seeded one.</summary>
    public interface IRandom
    {
        /// <summary>A number in [0, 1), like Math.random() in the prototype.</summary>
        double NextDouble();
    }

    public static class RandomExt
    {
        /// <summary>Whole number in [0, n), same as Math.floor(rng() * n) in the prototype.</summary>
        public static int Range(this IRandom rng, int n) => (int)(rng.NextDouble() * n);
    }

    /// <summary>xorshift32, the same generator as prototype/sim.js, so a seed gives the same game in both.</summary>
    public sealed class XorShift32 : IRandom
    {
        public uint State { get; private set; }

        public XorShift32(uint seed) { State = seed == 0 ? 1u : seed; }

        public uint NextUInt()
        {
            uint s = State;
            s ^= s << 13;
            s ^= s >> 17;
            s ^= s << 5;
            State = s;
            return s;
        }

        public double NextDouble() => NextUInt() / 4294967296.0;
    }

    /// <summary>System.Random wrapper for normal play.</summary>
    public sealed class SystemRandom : IRandom
    {
        private readonly System.Random _r;
        public SystemRandom() { _r = new System.Random(); }
        public SystemRandom(int seed) { _r = new System.Random(seed); }
        public double NextDouble() => _r.NextDouble();
    }
}
