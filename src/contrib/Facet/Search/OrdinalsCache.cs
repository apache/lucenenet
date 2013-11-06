using Lucene.Net.Facet.Encoding;
using Lucene.Net.Facet.Params;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class OrdinalsCache
    {
        public sealed class CachedOrds
        {
            public readonly int[] offsets;
            public readonly int[] ordinals;

            public CachedOrds(BinaryDocValues dv, int maxDoc, CategoryListParams clp)
            {
                BytesRef buf = new BytesRef();
                offsets = new int[maxDoc + 1];
                int[] ords = new int[maxDoc];
                int totOrds = 0;
                IntDecoder decoder = clp.CreateEncoder().CreateMatchingDecoder();
                IntsRef values = new IntsRef(32);
                for (int docID = 0; docID < maxDoc; docID++)
                {
                    offsets[docID] = totOrds;
                    dv.Get(docID, buf);
                    if (buf.length > 0)
                    {
                        decoder.Decode(buf, values);
                        if (totOrds + values.length >= ords.Length)
                        {
                            ords = ArrayUtil.Grow(ords, totOrds + values.length + 1);
                        }

                        for (int i = 0; i < values.length; i++)
                        {
                            ords[totOrds++] = values.ints[i];
                        }
                    }
                }

                offsets[maxDoc] = totOrds;
                if ((double)totOrds / ords.Length < 0.9)
                {
                    this.ordinals = new int[totOrds];
                    Array.Copy(ords, 0, this.ordinals, 0, totOrds);
                }
                else
                {
                    this.ordinals = ords;
                }
            }
        }

        private static readonly IDictionary<BinaryDocValues, CachedOrds> intsCache = new WeakDictionary<BinaryDocValues, CachedOrds>();

        public static CachedOrds GetCachedOrds(AtomicReaderContext context, CategoryListParams clp)
        {
            lock (typeof(OrdinalsCache))
            {
                BinaryDocValues dv = context.AtomicReader.GetBinaryDocValues(clp.field);
                if (dv == null)
                {
                    return null;
                }

                CachedOrds ci = intsCache[dv];
                if (ci == null)
                {
                    ci = new CachedOrds(dv, context.AtomicReader.MaxDoc, clp);
                    intsCache[dv] = ci;
                }

                return ci;
            }
        }
    }
}
