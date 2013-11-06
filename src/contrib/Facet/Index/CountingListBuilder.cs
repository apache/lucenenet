using Lucene.Net.Facet.Encoding;
using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Index
{
    public class CountingListBuilder : ICategoryListBuilder
    {
        private abstract class OrdinalsEncoder
        {
            internal OrdinalsEncoder()
            {
            }

            public abstract IDictionary<string, BytesRef> Encode(IntsRef ordinals);
        }

        private sealed class NoPartitionsOrdinalsEncoder : OrdinalsEncoder
        {
            private readonly IntEncoder encoder;
            private readonly string name = @"";
            
            internal NoPartitionsOrdinalsEncoder(CategoryListParams categoryListParams)
            {
                encoder = categoryListParams.CreateEncoder();
            }

            public override IDictionary<string, BytesRef> Encode(IntsRef ordinals)
            {
                BytesRef bytes = new BytesRef(128);
                encoder.Encode(ordinals, bytes);
                return new Dictionary<string, BytesRef>() { { name, bytes } };
            }
        }

        private sealed class PerPartitionOrdinalsEncoder : OrdinalsEncoder
        {
            private readonly FacetIndexingParams indexingParams;
            private readonly CategoryListParams categoryListParams;
            private readonly int partitionSize;
            private readonly HashMap<String, IntEncoder> partitionEncoder = new HashMap<String, IntEncoder>();

            internal PerPartitionOrdinalsEncoder(FacetIndexingParams indexingParams, CategoryListParams categoryListParams)
            {
                this.indexingParams = indexingParams;
                this.categoryListParams = categoryListParams;
                this.partitionSize = indexingParams.PartitionSize;
            }

            public override IDictionary<String, BytesRef> Encode(IntsRef ordinals)
            {
                HashMap<String, IntsRef> partitionOrdinals = new HashMap<String, IntsRef>();
                for (int i = 0; i < ordinals.length; i++)
                {
                    int ordinal = ordinals.ints[i];
                    string name = PartitionsUtils.PartitionNameByOrdinal(indexingParams, ordinal);
                    IntsRef partitionOrds = partitionOrdinals[name];
                    if (partitionOrds == null)
                    {
                        partitionOrds = new IntsRef(32);
                        partitionOrdinals[name] = partitionOrds;
                        partitionEncoder[name] = categoryListParams.CreateEncoder();
                    }

                    partitionOrds.ints[partitionOrds.length++] = ordinal % partitionSize;
                }

                HashMap<String, BytesRef> partitionBytes = new HashMap<String, BytesRef>();
                foreach (KeyValuePair<String, IntsRef> e in partitionOrdinals)
                {
                    string name = e.Key;
                    IntEncoder encoder = partitionEncoder[name];
                    BytesRef bytes = new BytesRef(128);
                    encoder.Encode(e.Value, bytes);
                    partitionBytes[name] = bytes;
                }

                return partitionBytes;
            }
        }

        private readonly OrdinalsEncoder ordinalsEncoder;
        private readonly ITaxonomyWriter taxoWriter;
        private readonly CategoryListParams clp;

        public CountingListBuilder(CategoryListParams categoryListParams, FacetIndexingParams indexingParams, ITaxonomyWriter taxoWriter)
        {
            this.taxoWriter = taxoWriter;
            this.clp = categoryListParams;
            if (indexingParams.PartitionSize == int.MaxValue)
            {
                ordinalsEncoder = new NoPartitionsOrdinalsEncoder(categoryListParams);
            }
            else
            {
                ordinalsEncoder = new PerPartitionOrdinalsEncoder(indexingParams, categoryListParams);
            }
        }

        public IDictionary<string, BytesRef> Build(IntsRef ordinals, IEnumerable<CategoryPath> categories)
        {
            int upto = ordinals.length;
            IEnumerator<CategoryPath> iter = categories.GetEnumerator();
            for (int i = 0; i < upto; i++)
            {
                int ordinal = ordinals.ints[i];
                iter.MoveNext();
                CategoryPath cp = iter.Current;
                CategoryListParams.OrdinalPolicy op = clp.GetOrdinalPolicy(cp.components[0]);
                if (op != CategoryListParams.OrdinalPolicy.NO_PARENTS)
                {
                    int parent = taxoWriter.GetParent(ordinal);
                    if (parent > 0)
                    {
                        while (parent > 0)
                        {
                            ordinals.ints[ordinals.length++] = parent;
                            parent = taxoWriter.GetParent(parent);
                        }

                        if (op == CategoryListParams.OrdinalPolicy.ALL_BUT_DIMENSION)
                        {
                            ordinals.length--;
                        }
                    }
                }
            }

            return ordinalsEncoder.Encode(ordinals);
        }
    }
}
