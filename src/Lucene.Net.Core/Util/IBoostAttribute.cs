using Lucene.Net.Util;

namespace Lucene.Net.Search
{
    // LUCENENET TODO: Move to Search directory, add documentation
    public interface IBoostAttribute : IAttribute
    {
        float Boost { get; set; }
    }
}