using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public interface IScoredDocIDsIterator
    {
        /** Iterate to the next document/score pair. Returns true iff there is such a pair. */
        bool Next();

        /** Returns the ID of the current document. */
        int DocID { get; }

        /** Returns the score of the current document. */
        float Score { get; }

    }

    public static class ScoredDocIDsIterator
    {
        /** Default score used in case scoring is disabled. */
        public const float DEFAULT_SCORE = 1.0f;
    }
}
