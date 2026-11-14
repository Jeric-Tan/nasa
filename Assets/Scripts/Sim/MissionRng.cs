using System;
using System.Collections.Generic;

namespace OutpostGale.Sim
{
    // Seeded random numbers whose state can be saved. All game randomness goes through this
    // (never UnityEngine.Random), so a Classroom Code replays the same mission.
    [Serializable]
    public class MissionRng
    {
        public uint state;

        public MissionRng() { }

        public MissionRng(int seed)
        {
            state = (uint)seed ^ 0x9E3779B9u;
            if (state == 0) state = 0x6D2B79F5u;
            for (int i = 0; i < 4; i++) NextUInt();
        }

        public uint NextUInt()
        {
            // xorshift32
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        // [0, 1)
        public double NextDouble() => (NextUInt() >> 8) / 16777216.0;

        public bool Chance(double p) => NextDouble() < p;

        // [min, max)
        public int Range(int min, int max) => max <= min ? min : min + (int)(NextDouble() * (max - min));

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                T t = list[i]; list[i] = list[j]; list[j] = t;
            }
        }

        public T Pick<T>(IList<T> list) => list[Range(0, list.Count)];

        public static int SeedFromCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return 0;
            unchecked
            {
                int h = (int)2166136261;
                foreach (char c in code.Trim().ToUpperInvariant())
                {
                    h ^= c;
                    h *= 16777619;
                }
                return h == 0 ? 1 : h;
            }
        }

        // A fresh 6-letter Classroom Code (no 0/O or 1/I to avoid mix-ups).
        public static string NewCode(Random random)
        {
            const string letters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var chars = new char[6];
            for (int i = 0; i < 6; i++) chars[i] = letters[random.Next(letters.Length)];
            return new string(chars);
        }
    }
}
