namespace Lucene.Net.Search
{
    public class NGramPhraseQuery : PhraseQuery
    {
        private readonly int n;

        public NGramPhraseQuery(int n)
        {
            this.n = n;
        }

        public override Query Rewrite(Index.IndexReader reader)
        {
            if (Slop != 0) return base.Rewrite(reader);

            var positions = GetPositions();
            var terms = GetTerms();
            var prevPosition = positions[0];
            for (var i = 1; i < positions.Length; i++)
            {
                var p = positions[i];
                if (prevPosition + 1 != p) return base.Rewrite(reader);
                prevPosition = p;
            }

            var optimized = new PhraseQuery();
            optimized.Boost = Boost;
            var pos = 0;
            var lastPos = terms.Length - 1;
            for (var i = 0; i < terms.Length; i++)
            {
                if (pos%n == 0 || pos >= lastPos)
                {
                    optimized.Add(terms[i], positions[i]);
                }
                pos++;
            }

            return optimized;
        }

        public override bool Equals(object o)
        {
            if (!(o is NGramPhraseQuery)) return false;

            var other = o as NGramPhraseQuery;
            return n == other.n && base.Equals(other);
        }

        public override int GetHashCode()
        {
            return Support.Number.FloatToIntBits(Boost)
                   ^ Slop
                   ^ GetTerms().GetHashCode()
                   ^ GetPositions().GetHashCode()
                   ^ n;
        }
    }
}
