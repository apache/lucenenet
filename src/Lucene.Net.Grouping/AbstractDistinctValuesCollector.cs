using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Search.Grouping
{
    /// <summary>
    /// A second pass grouping collector that keeps track of distinct values for a specified field for the top N group.
    /// 
    /// @lucene.experimental
    /// </summary>
    /// <typeparam name="GC"></typeparam>
    public abstract class AbstractDistinctValuesCollector<GC> : Collector, IAbstractDistinctValuesCollector<GC>
        where GC : IGroupCount /* AbstractDistinctValuesCollector<GC>.GroupCount */
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

    //public abstract class AbstractDistinctValuesCollector : Collector
    //{
    //    /// <summary>
    //    /// Returns all unique values for each top N group.
    //    /// </summary>
    //    /// <returns>all unique values for each top N group</returns>
    //    public abstract List<GC> GetGroups();

    //    public override bool AcceptsDocsOutOfOrder()
    //    {
    //        return true;
    //    }

    //    public override Scorer Scorer
    //    {
    //        set
    //        {
    //        }
    //    }
    //}


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
    public abstract class AbstractGroupCount<TGroupValue> : IGroupCount<TGroupValue>
        //where TGroupValue : IComparable
    {
        public TGroupValue GroupValue { get; protected set; }
        public IEnumerable<TGroupValue> UniqueValues { get; protected set; }

        public AbstractGroupCount(TGroupValue groupValue)
        {
            this.GroupValue = groupValue;
            this.UniqueValues = new HashSet<TGroupValue>();
        }
    }

    /// <summary>
    /// LUCENENET specific interface to allow usage of <see cref="AbstractGroupCount{TGroupValue}"/>
    /// as a generic closing type without having to specify TGroupValue.
    /// </summary>
    public interface IGroupCount
    {
    }


    /// <summary>
    /// LUCENENET specific interface used to apply covariance to TGroupValue
    /// </summary>
    /// <typeparam name="TGroupValue"></typeparam>
    public interface IGroupCount<out TGroupValue> : IGroupCount
    {
        TGroupValue GroupValue { get; }
        IEnumerable<TGroupValue> UniqueValues { get; }
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
