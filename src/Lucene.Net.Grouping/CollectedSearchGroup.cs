using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Search.Grouping
{
    /// <summary>
    /// Expert: representation of a group in {@link AbstractFirstPassGroupingCollector},
    /// tracking the top doc and {@link FieldComparator} slot.
    /// @lucene.internal
    /// </summary>
    /// <typeparam name="TGroupValue"></typeparam>
    public class CollectedSearchGroup<TGroupValue> : SearchGroup<TGroupValue>, ICollectedSearchGroup
    {
        public int TopDoc { get; internal set; }
        public int ComparatorSlot { get; internal set; }
    }


    /// <summary>
    /// LUCENENET specific interface for passing/comparing the CollectedSearchGroup
    /// without referencing its generic type
    /// </summary>
    public interface ICollectedSearchGroup
    {
        int TopDoc { get; }
        int ComparatorSlot { get; }
    }
}
