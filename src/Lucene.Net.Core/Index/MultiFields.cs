using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lucene.Net.Index
{
    using Lucene.Net.Util;

    /*
         * Licensed to the Apache Software Foundation (ASF) under one or more
         * contributor license agreements.  See the NOTICE file distributed with
         * this work for additional information regarding copyright ownership.
         * The ASF licenses this file to You under the Apache License, Version 2.0
         * (the "License"); you may not use this file except in compliance with
         * the License.  You may obtain a copy of the License at
         *
         *     http://www.apache.org/licenses/LICENSE-2.0
         *
         * Unless required by applicable law or agreed to in writing, software
         * distributed under the License is distributed on an "AS IS" BASIS,
         * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
         * See the License for the specific language governing permissions and
         * limitations under the License.
         */

    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Exposes flex API, merged from flex API of sub-segments.
    /// this is useful when you're interacting with an {@link
    /// IndexReader} implementation that consists of sequential
    /// sub-readers (eg <seealso cref="DirectoryReader"/> or {@link
    /// MultiReader}).
    ///
    /// <p><b>NOTE</b>: for composite readers, you'll get better
    /// performance by gathering the sub readers using
    /// <seealso cref="IndexReader#getContext()"/> to get the
    /// atomic leaves and then operate per-AtomicReader,
    /// instead of using this class.
    ///
    /// @lucene.experimental
    /// </summary>

    public sealed class MultiFields : Fields
    {
        private readonly Fields[] subs;
        private readonly ReaderSlice[] subSlices;
        private readonly IDictionary<string, Terms> terms = new ConcurrentDictionary<string, Terms>();

        /// <summary>
        /// Returns a single <seealso cref="Fields"/> instance for this
        ///  reader, merging fields/terms/docs/positions on the
        ///  fly.  this method will return null if the reader
        ///  has no postings.
        ///
        ///  <p><b>NOTE</b>: this is a slow way to access postings.
        ///  It's better to get the sub-readers and iterate through them
        ///  yourself.
        /// </summary>
        public static Fields GetFields(IndexReader reader)
        {
            var leaves = reader.Leaves;
            switch (leaves.Count)
            {
                case 0:
                    // no fields
                    return null;

                case 1:
                    // already an atomic reader / reader with one leave
                    return leaves[0].AtomicReader.Fields;

                default:
                    IList<Fields> fields = new List<Fields>();
                    IList<ReaderSlice> slices = new List<ReaderSlice>();
                    foreach (AtomicReaderContext ctx in leaves)
                    {
                        AtomicReader r = ctx.AtomicReader;
                        Fields f = r.Fields;
                        if (f != null)
                        {
                            fields.Add(f);
                            slices.Add(new ReaderSlice(ctx.DocBase, r.MaxDoc, fields.Count - 1));
                        }
                    }
                    if (fields.Count == 0)
                    {
                        return null;
                    }
                    else if (fields.Count == 1)
                    {
                        return fields[0];
                    }
                    else
                    {
                        return new MultiFields(fields.ToArray(/*Fields.EMPTY_ARRAY*/), slices.ToArray(/*ReaderSlice.EMPTY_ARRAY*/));
                    }
            }
        }

        /// <summary>
        /// Returns a single <seealso cref="Bits"/> instance for this
        ///  reader, merging live Documents on the
        ///  fly.  this method will return null if the reader
        ///  has no deletions.
        ///
        ///  <p><b>NOTE</b>: this is a very slow way to access live docs.
        ///  For example, each Bits access will require a binary search.
        ///  It's better to get the sub-readers and iterate through them
        ///  yourself.
        /// </summary>
        public static Bits GetLiveDocs(IndexReader reader)
        {
            if (reader.HasDeletions)
            {
                IList<AtomicReaderContext> leaves = reader.Leaves;
                int size = leaves.Count;
                Debug.Assert(size > 0, "A reader with deletions must have at least one leave");
                if (size == 1)
                {
                    return leaves[0].AtomicReader.LiveDocs;
                }
                var liveDocs = new Bits[size];
                int[] starts = new int[size + 1];
                for (int i = 0; i < size; i++)
                {
                    // record all liveDocs, even if they are null
                    AtomicReaderContext ctx = leaves[i];
                    liveDocs[i] = ctx.AtomicReader.LiveDocs;
                    starts[i] = ctx.DocBase;
                }
                starts[size] = reader.MaxDoc;
                return new MultiBits(liveDocs, starts, true);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        ///  this method may return null if the field does not exist. </summary>
        public static Terms GetTerms(IndexReader r, string field)
        {
            Fields fields = GetFields(r);
            if (fields == null)
            {
                return null;
            }
            else
            {
                return fields.Terms(field);
            }
        }

        /// <summary>
        /// Returns <seealso cref="DocsEnum"/> for the specified field &
        ///  term.  this will return null if the field or term does
        ///  not exist.
        /// </summary>
        public static DocsEnum GetTermDocsEnum(IndexReader r, Bits liveDocs, string field, BytesRef term)
        {
            return GetTermDocsEnum(r, liveDocs, field, term, DocsEnum.FLAG_FREQS);
        }

        /// <summary>
        /// Returns <seealso cref="DocsEnum"/> for the specified field &
        ///  term, with control over whether freqs are required.
        ///  Some codecs may be able to optimize their
        ///  implementation when freqs are not required.  this will
        ///  return null if the field or term does not exist.  See {@link
        ///  TermsEnum#docs(Bits,DocsEnum,int)}.
        /// </summary>
        public static DocsEnum GetTermDocsEnum(IndexReader r, Bits liveDocs, string field, BytesRef term, int flags)
        {
            Debug.Assert(field != null);
            Debug.Assert(term != null);
            Terms terms = GetTerms(r, field);
            if (terms != null)
            {
                TermsEnum termsEnum = terms.Iterator(null);
                if (termsEnum.SeekExact(term))
                {
                    return termsEnum.Docs(liveDocs, null, flags);
                }
            }
            return null;
        }

        /// <summary>
        /// Returns <seealso cref="DocsAndPositionsEnum"/> for the specified
        ///  field & term.  this will return null if the field or
        ///  term does not exist or positions were not indexed. </summary>
        ///  <seealso cref= #getTermPositionsEnum(IndexReader, Bits, String, BytesRef, int)  </seealso>
        public static DocsAndPositionsEnum GetTermPositionsEnum(IndexReader r, Bits liveDocs, string field, BytesRef term)
        {
            return GetTermPositionsEnum(r, liveDocs, field, term, DocsAndPositionsEnum.FLAG_OFFSETS | DocsAndPositionsEnum.FLAG_PAYLOADS);
        }

        /// <summary>
        /// Returns <seealso cref="DocsAndPositionsEnum"/> for the specified
        ///  field & term, with control over whether offsets and payloads are
        ///  required.  Some codecs may be able to optimize
        ///  their implementation when offsets and/or payloads are not
        ///  required. this will return null if the field or term does not
        ///  exist or positions were not indexed. See {@link
        ///  TermsEnum#docsAndPositions(Bits,DocsAndPositionsEnum,int)}.
        /// </summary>
        public static DocsAndPositionsEnum GetTermPositionsEnum(IndexReader r, Bits liveDocs, string field, BytesRef term, int flags)
        {
            Debug.Assert(field != null);
            Debug.Assert(term != null);
            Terms terms = GetTerms(r, field);
            if (terms != null)
            {
                TermsEnum termsEnum = terms.Iterator(null);
                if (termsEnum.SeekExact(term))
                {
                    return termsEnum.DocsAndPositions(liveDocs, null, flags);
                }
            }
            return null;
        }

        /// <summary>
        /// Expert: construct a new MultiFields instance directly.
        /// @lucene.internal
        /// </summary>
        // TODO: why is this public?
        public MultiFields(Fields[] subs, ReaderSlice[] subSlices)
        {
            this.subs = subs;
            this.subSlices = subSlices;
        }

        public override IEnumerator<string> GetEnumerator()
        {
            IEnumerator<string>[] subIterators = new IEnumerator<string>[subs.Length];
            for (int i = 0; i < subs.Length; i++)
            {
                subIterators[i] = subs[i].GetEnumerator();
            }
            return new MergedIterator<string>(subIterators);
        }

        public override Terms Terms(string field)
        {
            Terms result;
            terms.TryGetValue(field, out result);
            if (result != null)
            {
                return result;
            }

            // Lazy init: first time this field is requested, we
            // create & add to terms:
            IList<Terms> subs2 = new List<Terms>();
            IList<ReaderSlice> slices2 = new List<ReaderSlice>();

            // Gather all sub-readers that share this field
            for (int i = 0; i < subs.Length; i++)
            {
                Terms terms = subs[i].Terms(field);
                if (terms != null)
                {
                    subs2.Add(terms);
                    slices2.Add(subSlices[i]);
                }
            }
            if (subs2.Count == 0)
            {
                result = null;
                // don't cache this case with an unbounded cache, since the number of fields that don't exist
                // is unbounded.
            }
            else
            {
                result = new MultiTerms(subs2.ToArray(/*Terms.EMPTY_ARRAY*/), slices2.ToArray(/*ReaderSlice.EMPTY_ARRAY*/));
                terms[field] = result;
            }

            return result;
        }

        public override int Size
        {
            get { return -1; }
        }

        /// <summary>
        /// Call this to get the (merged) FieldInfos for a
        ///  composite reader.
        ///  <p>
        ///  NOTE: the returned field numbers will likely not
        ///  correspond to the actual field numbers in the underlying
        ///  readers, and codec metadata (<seealso cref="FieldInfo#getAttribute(String)"/>
        ///  will be unavailable.
        /// </summary>
        public static FieldInfos GetMergedFieldInfos(IndexReader reader)
        {
            var builder = new FieldInfos.Builder();
            foreach (AtomicReaderContext ctx in reader.Leaves)
            {
                builder.Add(ctx.AtomicReader.FieldInfos);
            }
            return builder.Finish();
        }

        /// <summary>
        /// Call this to get the (merged) FieldInfos representing the
        ///  set of indexed fields <b>only</b> for a composite reader.
        ///  <p>
        ///  NOTE: the returned field numbers will likely not
        ///  correspond to the actual field numbers in the underlying
        ///  readers, and codec metadata (<seealso cref="FieldInfo#getAttribute(String)"/>
        ///  will be unavailable.
        /// </summary>
        public static ICollection<string> GetIndexedFields(IndexReader reader)
        {
            ICollection<string> fields = new HashSet<string>();
            foreach (FieldInfo fieldInfo in GetMergedFieldInfos(reader))
            {
                if (fieldInfo.IsIndexed)
                {
                    fields.Add(fieldInfo.Name);
                }
            }
            return fields;
        }
    }
}