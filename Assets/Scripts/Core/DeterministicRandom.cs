// Football Realm — Core (no UnityEngine dependency).
// Deterministic RNG: the same seed always produces the same season,
// the same match, and the same story outcomes. This is what makes
// saves reproducible and match results auditable — a requirement for
// a management game, not a cosmetic choice.
//
// Algorithm: xorshift64*. State must never be 0 (it is forced to a
// fixed non-zero value if it is). All draws are derived from Next()
// so there is exactly one source of randomness in the whole core.

using System;

namespace FootballRealm.Core
{
    /// <summary>Deterministic, seedable RNG for all simulation.</summary>
    public sealed class DeterministicRandom
    {
        private ulong _state;

        public DeterministicRandom(long seed)
        {
            _state = (ulong)seed;
            if (_state == 0) _state = 0x9E3779B97F4A7C15UL;
        }

        public ulong NextUInt64()
        {
            ulong x = _state;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            _state = x;
            return x * 0x2545F4914F6CDD1DUL;
        }

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            ulong range = (ulong)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt64() % range);
        }

        /// <summary>Uniform double in [0, 1).</summary>
        public double NextDouble()
        {
            // 53 bits of precision, the same shape as System.Random.
            return (NextUInt64() >> 11) * (1.0 / 9007199254740992.0);
        }

        /// <summary>True with the given probability (0..1).</summary>
        public bool Chance(double probability)
        {
            if (probability <= 0) return false;
            if (probability >= 1) return true;
            return NextDouble() < probability;
        }

        /// <summary>Normal-ish draw in [mean - spread, mean + spread].</summary>
        public double Around(double mean, double spread)
        {
            // Average of two uniforms: cheap, bounded, no tails.
            return mean + (NextDouble() + NextDouble() - 1.0) * spread;
        }

        public long SeedState => (long)_state;
    }

    /// <summary>
    /// Stable string hash (FNV-1a). Used to derive a club's squad seed
    /// from its name, so squads are the same on every platform and
    /// every run. Never use GetHashCode for this — it is not stable
    /// across runtimes.
    /// </summary>
    public static class StableHash
    {
        public static long Fnv1a(string text)
        {
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                foreach (char c in text ?? string.Empty)
                {
                    hash ^= (ulong)c;
                    hash *= 1099511628211UL;
                }
                return (long)(hash & 0x7FFFFFFFFFFFFFFFUL);
            }
        }
    }
}
