using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.Randomized
{
    public static class SeedUtils
    {
        private readonly static char[] HEX = "0123456789ABCDEF".ToCharArray();

        public static int ParseSeed(string seed)
        {
            int result = 0;
            foreach (var item in seed.ToCharArray())
            {
                var character = char.ToLower(item); // LUCENENET NOTE: Intentionally using current culture
                result = result << 4;
                if (character >= '0' && character <= '9')
                    result |= (character - '0');
                else if (character >= 'a' && character <= 'f')
                    result |= (character - 'a' + 10);
                else
                    throw new ArgumentException("Expected the seed to be in a hexadecimal format: " + seed);
            }

            return result;
        }

        public static string FormatSeed(int seed)
        {
            var sb = new StringBuilder();
            do
            {
                sb.Append(HEX[(int)(seed & 0xf)]);
                seed = seed >> 4;
            } while (seed != 0);

            return sb.ToString().Reverse().ToString();
        }

        public static int[] ParseSeedChain(String chain)
        {
            if (chain == null)
                throw new ArgumentNullException("chain");

            chain = chain.Replace("[", "").Replace("]", "");
            var matches = Regex.Matches("[0-9A-Fa-f\\:]+", chain);
            if (matches.Count == 0)
                throw new ArgumentException("Not a valid seed chain: " + chain, "chain");

            var parts = chain.Split(":".ToCharArray());
            int[] result = new int[parts.Length];

            for (int i = 0; i < parts.Length; i++)
                result[i] = ParseSeed(parts[i]);

            return result;
        }

        public static string FormatStringChain(params Randomness[] values)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                    sb.Append(":");
                sb.Append(FormatSeed(values[i].Seed));
            }

            return sb.ToString();
        }
    }
}