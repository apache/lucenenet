using Lucene.Net.Facet.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public abstract class FacetRequest
    {
        public enum ResultMode
        {
            PER_NODE_IN_TREE,
            GLOBAL_FLAT
        }

        public enum FacetArraysSource
        {
            INT,
            FLOAT,
            BOTH
        }

        public enum SortOrder
        {
            ASCENDING,
            DESCENDING
        }

        public static readonly int DEFAULT_DEPTH = 1;
        public static readonly ResultMode DEFAULT_RESULT_MODE = ResultMode.PER_NODE_IN_TREE;
        public readonly CategoryPath categoryPath;
        public readonly int numResults;
        private int numLabel;
        private int depth;
        private SortOrder sortOrder;
        private readonly int hashCode;
        private ResultMode resultMode = DEFAULT_RESULT_MODE;

        public FacetRequest(CategoryPath path, int numResults)
        {
            if (numResults <= 0)
            {
                throw new ArgumentException(@"num results must be a positive (>0) number: " + numResults);
            }

            if (path == null)
            {
                throw new ArgumentException(@"category path cannot be null!");
            }

            categoryPath = path;
            this.numResults = numResults;
            numLabel = numResults;
            depth = DEFAULT_DEPTH;
            sortOrder = SortOrder.DESCENDING;
            hashCode = categoryPath.GetHashCode() ^ this.numResults;
        }

        public virtual IAggregator CreateAggregator(bool useComplements, FacetArrays arrays, TaxonomyReader taxonomy)
        {
            throw new NotSupportedException(@"this FacetRequest does not support this type of Aggregator anymore; " + @"you should override FacetsAccumulator to return the proper FacetsAggregator");
        }

        public override bool Equals(Object o)
        {
            if (o is FacetRequest)
            {
                FacetRequest that = (FacetRequest)o;
                return that.hashCode == this.hashCode 
                    && that.categoryPath.Equals(this.categoryPath) 
                    && that.numResults == this.numResults 
                    && that.depth == this.depth 
                    && that.resultMode == this.resultMode 
                    && that.numLabel == this.numLabel;
            }

            return false;
        }

        public virtual int Depth
        {
            get
            {
                return depth;
            }
            set
            {
                this.depth = value;
            }
        }

        public abstract FacetArraysSource FacetArraysSourceValue { get; }

        public virtual int NumLabel
        {
            get
            {
                return numLabel;
            }
            set
            {
                this.numLabel = value;
            }
        }

        public virtual ResultMode ResultModeValue
        {
            get
            {
                return resultMode;
            }
            set
            {
                this.resultMode = value;
            }
        }

        public virtual SortOrder SortOrderValue
        {
            get
            {
                return sortOrder;
            }
            set
            {
                this.sortOrder = value;
            }
        }

        public abstract double GetValueOf(FacetArrays arrays, int idx);

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override string ToString()
        {
            return categoryPath.ToString() + @" nRes=" + numResults + @" nLbl=" + numLabel;
        }
    }
}
