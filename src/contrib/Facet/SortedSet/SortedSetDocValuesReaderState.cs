using Lucene.Net.Facet.Params;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.Facet.SortedSet
{
    public sealed class SortedSetDocValuesReaderState
    {
        private readonly string field;
        private readonly AtomicReader topReader;
        private readonly int valueCount;
        internal readonly char separator;
        internal readonly string separatorRegex;
        public static readonly string FACET_FIELD_EXTENSION = @"_sorted_doc_values";

        internal sealed class OrdRange
        {
            public readonly int start;
            public readonly int end;
            public OrdRange(int start, int end)
            {
                this.start = start;
                this.end = end;
            }
        }

        private readonly IDictionary<String, OrdRange> prefixToOrdRange = new HashMap<String, OrdRange>();

        public SortedSetDocValuesReaderState(IndexReader reader)
            : this(FacetIndexingParams.DEFAULT, reader)
        {
        }

        public SortedSetDocValuesReaderState(FacetIndexingParams fip, IndexReader reader)
        {
            this.field = fip.GetCategoryListParams(null).field + FACET_FIELD_EXTENSION;
            this.separator = fip.FacetDelimChar;
            this.separatorRegex = Regex.Escape(separator.ToString());
            if (reader is AtomicReader)
            {
                topReader = (AtomicReader)reader;
            }
            else
            {
                topReader = new SlowCompositeReaderWrapper((CompositeReader)reader);
            }

            SortedSetDocValues dv = topReader.GetSortedSetDocValues(field);
            if (dv == null)
            {
                throw new ArgumentException("field \"" + field + "\" was not indexed with SortedSetDocValues");
            }

            if (dv.ValueCount > int.MaxValue)
            {
                throw new ArgumentException(@"can only handle valueCount < Integer.MAX_VALUE; got " + dv.ValueCount);
            }

            valueCount = (int)dv.ValueCount;
            string lastDim = null;
            int startOrd = -1;
            BytesRef spare = new BytesRef();
            for (int ord = 0; ord < valueCount; ord++)
            {
                dv.LookupOrd(ord, spare);
                String[] components = spare.Utf8ToString().Split(new[] { separatorRegex}, StringSplitOptions.None);
                if (components.Length != 2)
                {
                    throw new ArgumentException(@"this class can only handle 2 level hierarchy (dim/value); got: " + spare.Utf8ToString());
                }

                if (!components[0].Equals(lastDim))
                {
                    if (lastDim != null)
                    {
                        prefixToOrdRange[lastDim] = new OrdRange(startOrd, ord - 1);
                    }

                    startOrd = ord;
                    lastDim = components[0];
                }
            }

            if (lastDim != null)
            {
                prefixToOrdRange[lastDim] = new OrdRange(startOrd, valueCount - 1);
            }
        }

        internal SortedSetDocValues DocValues
        {
            get
            {
                return topReader.GetSortedSetDocValues(field);
            }
        }

        internal OrdRange GetOrdRange(string dim)
        {
            return prefixToOrdRange[dim];
        }

        internal string Field
        {
            get
            {
                return field;
            }
        }

        internal int Size
        {
            get
            {
                return valueCount;
            }
        }
    }
}
