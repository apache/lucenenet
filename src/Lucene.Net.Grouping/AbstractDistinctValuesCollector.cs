using System.Collections.Generic;

namespace Lucene.Net.Search.Grouping
{
    /// <summary>
    /// A second pass grouping collector that keeps track of distinct values for a specified field for the top N group.
    /// 
    /// @lucene.experimental
    /// </summary>
    /// <typeparam name="GC"></typeparam>
    public abstract class AbstractDistinctValuesCollector<GC> : Collector, IAbstractDistinctValuesCollector<GC>
        where GC : AbstractDistinctValuesCollector.IGroupCount<object>
    {
        /// <summary>
        /// Returns all unique values for each top N group.
        /// </summary>
        /// <returns>all unique values for each top N group</returns>
        public abstract IEnumerable<GC> Groups { get; }

        public override bool AcceptsDocsOutOfOrder()
        {
            return true;
        }

        public override Scorer Scorer
        {
            set
            {
            }
        } 
    }

    /// <summary>
    /// LUCENENET specific class used to nest the <see cref="GroupCount{TGroupValue}"/>
    /// class so it has similar syntax to that in Java Lucene
    /// (AbstractDistinctValuesCollector.GroupCount{TGroupValue} rather than 
    /// AbstractDistinctValuesCollector{GC}.GroupCount{TGroupValue}).
    /// </summary>
    public class AbstractDistinctValuesCollector
    {
        // Disallow direct creation
        private AbstractDistinctValuesCollector() { }

        /// <summary>
        /// Returned by <see cref="AbstractDistinctValuesCollector.GetGroups()"/>,
        /// representing the value and set of distinct values for the group.
        /// </summary>
        /// <typeparam name="TGroupValue"></typeparam>
        /// <remarks>
        /// LUCENENET - removed this class from being a nested class of 
        /// <see cref="AbstractDistinctValuesCollector{GC}"/> and renamed
        /// from GroupCount to AbstractGroupCount
        /// </remarks>
        public abstract class GroupCount<TGroupValue> : IGroupCount<TGroupValue>
        {
            public TGroupValue GroupValue { get; protected set; }
            public IEnumerable<TGroupValue> UniqueValues { get; protected set; }

            public GroupCount(TGroupValue groupValue)
            {
                this.GroupValue = groupValue;
                this.UniqueValues = new HashSet<TGroupValue>();
            }
        }

        /// <summary>
        /// LUCENENET specific interface used to apply covariance to TGroupValue
        /// </summary>
        /// <typeparam name="TGroupValue"></typeparam>
        public interface IGroupCount<out TGroupValue>
        {
            TGroupValue GroupValue { get; }
            IEnumerable<TGroupValue> UniqueValues { get; }
        }
    }

    /// <summary>
    /// LUCENENET specific interface used to apply covariance to GC
    /// </summary>
    /// <typeparam name="GC"></typeparam>
    public interface IAbstractDistinctValuesCollector<out GC>
    {
        IEnumerable<GC> Groups { get; }
    }
}
