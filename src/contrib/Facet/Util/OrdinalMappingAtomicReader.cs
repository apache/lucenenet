using Lucene.Net.Facet.Encoding;
using Lucene.Net.Facet.Params;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Util
{
    
    public class OrdinalMappingAtomicReader : FilterAtomicReader
    {
        private readonly int[] ordinalMap;
        private readonly IDictionary<String, CategoryListParams> dvFieldMap = new HashMap<String, CategoryListParams>();
        
        public OrdinalMappingAtomicReader(AtomicReader in_renamed, int[] ordinalMap)
            : this (in_renamed, ordinalMap, FacetIndexingParams.DEFAULT)
        {
        }

        public OrdinalMappingAtomicReader(AtomicReader in_renamed, int[] ordinalMap, FacetIndexingParams indexingParams)
            : base (in_renamed)
        {
            this.ordinalMap = ordinalMap;
            foreach (CategoryListParams params_renamed in indexingParams.AllCategoryListParams)
            {
                dvFieldMap[params_renamed.field] = params_renamed;
            }
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            BinaryDocValues inner = base.GetBinaryDocValues(field);
            if (inner == null)
            {
                return inner;
            }

            CategoryListParams clp = dvFieldMap[field];
            if (clp == null)
            {
                return inner;
            }
            else
            {
                return new OrdinalMappingBinaryDocValues(this, clp, inner);
            }
        }

        private class OrdinalMappingBinaryDocValues : BinaryDocValues
        {
            private readonly IntEncoder encoder;
            private readonly IntDecoder decoder;
            private readonly IntsRef ordinals = new IntsRef(32);
            private readonly BinaryDocValues delegate_renamed;
            private readonly BytesRef scratch = new BytesRef();
            private readonly OrdinalMappingAtomicReader parent;

            internal OrdinalMappingBinaryDocValues(OrdinalMappingAtomicReader parent, CategoryListParams clp, BinaryDocValues delegate_renamed)
            {
                this.parent = parent;
                this.delegate_renamed = delegate_renamed;
                encoder = clp.CreateEncoder();
                decoder = encoder.CreateMatchingDecoder();                
            }

            public override void Get(int docID, BytesRef result)
            {
                delegate_renamed.Get(docID, scratch);
                if (scratch.length > 0)
                {
                    decoder.Decode(scratch, ordinals);
                    for (int i = 0; i < ordinals.length; i++)
                    {
                        ordinals.ints[i] = parent.ordinalMap[ordinals.ints[i]];
                    }

                    encoder.Encode(ordinals, result);
                }
            }
        }
    }
}
