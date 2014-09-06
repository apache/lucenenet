using Lucene.Net.Util;

namespace Lucene.Net.Search
{
    public interface IBoostAttribute : IAttribute
    {
        float Boost { get; set; }
    }
}