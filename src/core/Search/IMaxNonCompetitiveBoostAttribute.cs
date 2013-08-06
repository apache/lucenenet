using Lucene.Net.Util;

namespace Lucene.Net.Search
{
    public interface IMaxNonCompetitiveBoostAttribute : IAttribute
    {
        float MaxNonCompetitiveBoost { get; set; }

        BytesRef CompetitiveTerm { get; set; }
    }
}
