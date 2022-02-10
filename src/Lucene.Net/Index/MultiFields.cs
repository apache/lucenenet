using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Index
{
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using IBits = Lucene.Net.Util.IBits;

    /// <summary>
    /// Exposes flex API, merged from flex API of sub-segments.
    /// This is useful when you're interacting with an 
    /// <see cref="IndexReader"/> implementation that consists of sequential
    /// sub-readers (eg <see cref="DirectoryReader"/> or 
    /// <see cref="MultiReader"/>).
    ///
    /// <para/><b>NOTE</b>: for composite readers, you'll get better
    /// performance by gathering the sub readers using
    /// <see cref="IndexReader.Context"/> to get the
    /// atomic leaves and then operate per-AtomicReader,
    /// instead of using this class.
    /// <para/>
    /// @lucene.experimental
    /// </summary>

    public sealed class MultiFields : Fields
    {
        private readonly Fields[] subs;
        private readonly ReaderSlice[] subSlices;
        private readonly IDictionary<string, Terms> terms = new ConcurrentDictionary<string, Terms>();

        /// <summary>
        /// Returns a single <see cref="Fields"/> instance for this
        /// reader, merging fields/terms/docs/positions on the
        /// fly.  This method will return <c>null</c> if the reader
        /// has no postings.
        ///
        /// <para/><b>NOTE</b>: this is a slow way to access postings.
        /// It's better to get the sub-readers and iterate through them
        /// yourself.
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
                    IList<Fields> fields = new JCG.List<Fields>();
                    IList<ReaderSlice> slices = new JCG.List<ReaderSlice>();
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
        /// Returns a single <see cref="IBits"/> instance for this
        /// reader, merging live Documents on the
        /// fly.  This method will return <c>null</c> if the reader
        /// has no deletions.
        ///
        /// <para/><b>NOTE</b>: this is a very slow way to access live docs.
        /// For example, each <see cref="IBits"/> access will require a binary search.
        /// It's better to get the sub-readers and iterate through them
        /// yourself.
        /// </summary>
        public static IBits GetLiveDocs(IndexReader reader)
        {
            if (reader.HasDeletions)
            {
                IList<AtomicReaderContext> leaves = reader.Leaves;
                int size = leaves.Count;
                if (Debugging.AssertsEnabled) Debugging.Assert(size > 0, "A reader with deletions must have at least one leave");
                if (size == 1)
                {
                    return leaves[0].AtomicReader.LiveDocs;
                }
                var liveDocs = new IBits[size];
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
        /// this method may return <c>null</c> if the field does not exist. </summary>
        public static Terms GetTerms(IndexReader r, string field)
        {
            Fields fields = GetFields(r);
            if (fields is null)
            {
                return null;
            }
            else
            {
                return fields.GetTerms(field);
            }
        }

        /// <summary>
        /// Returns <see cref="DocsEnum"/> for the specified field &amp;
        /// term.  This will return <c>null</c> if the field or term does
        /// not exist.
        /// </summary>
        public static DocsEnum GetTermDocsEnum(IndexReader r, IBits liveDocs, string field, BytesRef term)
        {
            return GetTermDocsEnum(r, liveDocs, field, term, DocsFlags.FREQS);
        }

        /// <summary>
        /// Returns <see cref="DocsEnum"/> for the specified field &amp;
        /// term, with control over whether freqs are required.
        /// Some codecs may be able to optimize their
        /// implementation when freqs are not required.  This will
        /// return <c>null</c> if the field or term does not exist.  See
        /// <see cref="TermsEnum.Docs(IBits, DocsEnum, DocsFlags)"/>.
        /// </summary>
        public static DocsEnum GetTermDocsEnum(IndexReader r, IBits liveDocs, string field, BytesRef term, DocsFlags flags)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(field != null);
                Debugging.Assert(term != null);
            }
            Terms terms = GetTerms(r, field);
            if (terms != null)
            {
                TermsEnum termsEnum = terms.GetEnumerator();
                if (termsEnum.SeekExact(term))
                {
                    return termsEnum.Docs(liveDocs, null, flags);
                }
            }
            return null;
        }

        /// <summary>
        /// Returns <see cref="DocsAndPositionsEnum"/> for the specified
        /// field &amp; term.  This will return <c>null</c> if the field or
        /// term does not exist or positions were not indexed. </summary>
        /// <seealso cref="GetTermPositionsEnum(IndexReader, IBits, string, BytesRef, DocsAndPositionsFlags)"/>
        public static DocsAndPositionsEnum GetTermPositionsEnum(IndexReader r, IBits liveDocs, string field, BytesRef term)
        {
            return GetTermPositionsEnum(r, liveDocs, field, term, DocsAndPositionsFlags.OFFSETS | DocsAndPositionsFlags.PAYLOADS);
        }

        /// <summary>
        /// Returns <see cref="DocsAndPositionsEnum"/> for the specified
        /// field &amp; term, with control over whether offsets and payloads are
        /// required.  Some codecs may be able to optimize
        /// their implementation when offsets and/or payloads are not
        /// required. This will return <c>null</c> if the field or term does not
        /// exist or positions were not indexed. See 
        /// <see cref="TermsEnum.DocsAndPositions(IBits, DocsAndPositionsEnum, DocsAndPositionsFlags)"/>.
        /// </summary>
        public static DocsAndPositionsEnum GetTermPositionsEnum(IndexReader r, IBits liveDocs, string field, BytesRef term, DocsAndPositionsFlags flags)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(field != null);
                Debugging.Assert(term != null);
            }
            Terms terms = GetTerms(r, field);
            if (terms != null)
            {
                TermsEnum termsEnum = terms.GetEnumerator();
                if (termsEnum.SeekExact(term))
                {
                    return termsEnum.DocsAndPositions(liveDocs, null, flags);
                }
            }
            return null;
        }

        /// <summary>
        /// Expert: construct a new <see cref="MultiFields"/> instance directly.
        /// <para/>
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
            return new MergedEnumerator<string>(subIterators);
        }

        public override Terms GetTerms(string field)
        {
            if (terms.TryGetValue(field, out Terms result) && result != null)
            {
                return result;
            }

            // Lazy init: first time this field is requested, we
            // create & add to terms:
            IList<Terms> subs2 = new JCG.List<Terms>();
            IList<ReaderSlice> slices2 = new JCG.List<ReaderSlice>();

            // Gather all sub-readers that share this field
            for (int i = 0; i < subs.Length; i++)
            {
                Terms terms = subs[i].GetTerms(field);
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

        public override int Count => -1;

        /// <summary>
        /// Call this to get the (merged) <see cref="FieldInfos"/> for a
        /// composite reader.
        /// <para/>
        /// NOTE: the returned field numbers will likely not
        /// correspond to the actual field numbers in the underlying
        /// readers, and codec metadata (<see cref="FieldInfo.GetAttribute(string)"/>)
        /// will be unavailable.
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
        /// Call this to get the (merged) <see cref="FieldInfos"/> representing the
        /// set of indexed fields <b>only</b> for a composite reader.
        /// <para/>
        /// NOTE: the returned field numbers will likely not
        /// correspond to the actual field numbers in the underlying
        /// readers, and codec metadata (<see cref="FieldInfo.GetAttribute(string)"/>)
        /// will be unavailable.
        /// </summary>
        public static ICollection<string> GetIndexedFields(IndexReader reader)
        {
            ICollection<string> fields = new JCG.HashSet<string>();
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