// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Queries
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
    
    /// <summary>
    /// Constructs a filter for docs matching any of the terms added to this class.
    /// Unlike a RangeFilter this can be used for filtering on multiple terms that are not necessarily in
    /// a sequence. An example might be a collection of primary keys from a database query result or perhaps
    /// a choice of "category" labels picked by the end user. As a filter, this is much faster than the
    /// equivalent query (a <see cref="BooleanQuery"/> with many "should" <see cref="TermQuery"/>s)
    /// </summary>
    public sealed class TermsFilter : Filter
    {
        /*
         * this class is often used for large number of terms in a single field.
         * to optimize for this case and to be filter-cache friendly we 
         * serialize all terms into a single byte array and store offsets
         * in a parallel array to keep the # of object constant and speed up
         * equals / hashcode.
         * 
         * This adds quite a bit of complexity but allows large term filters to
         * be efficient for GC and cache-lookups
         */
        private readonly int[] offsets;
        private readonly byte[] termsBytes;
        private readonly TermsAndField[] termsAndFields;
        private readonly int hashCode; // cached hashcode for fast cache lookups
        private const int PRIME = 31;

        /// <summary>
        /// Creates a new <see cref="TermsFilter"/> from the given list. The list
        /// can contain duplicate terms and multiple fields.
        /// </summary>
        public TermsFilter(IList<Term> terms)
            : this(new FieldAndTermEnumAnonymousClass(terms), terms.Count)
        {
        }

        private sealed class FieldAndTermEnumAnonymousClass : FieldAndTermEnum
        {            
            public FieldAndTermEnumAnonymousClass(IList<Term> terms)
            {
                // LUCENENET specific - added guard clause for null
                if (terms is null)
                    throw new ArgumentNullException(nameof(terms));

                if (terms.Count == 0)
                {
                    throw new ArgumentException("no terms provided");
                }

                terms.Sort();
                iter = terms.GetEnumerator();
            }

            // we need to sort for deduplication and to have a common cache key
            private readonly IEnumerator<Term> iter;
            public override bool MoveNext()
            {
                if (iter.MoveNext())
                {
                    var next = iter.Current;
                    Field = next.Field;
                    m_current = next.Bytes;
                    return true;
                }
                m_current = null;
                return false;
            }
        }

        /// <summary>
        /// Creates a new <see cref="TermsFilter"/> from the given <see cref="BytesRef"/> list for
        /// a single field.
        /// </summary>
        public TermsFilter(string field, IList<BytesRef> terms)
            : this(new FieldAndTermEnumAnonymousClass2(field, terms), terms.Count)
        {
        }

        private sealed class FieldAndTermEnumAnonymousClass2 : FieldAndTermEnum
        {
            public FieldAndTermEnumAnonymousClass2(string field, IList<BytesRef> terms)
                : base(field)
            {
                // LUCENENET specific - added guard clause for null
                if (terms is null)
                    throw new ArgumentNullException(nameof(terms));

                if (terms.Count == 0)
                {
                    throw new ArgumentException("no terms provided");
                }

                terms.Sort();
                iter = terms.GetEnumerator();
            }

            // we need to sort for deduplication and to have a common cache key
            private readonly IEnumerator<BytesRef> iter;
            public override bool MoveNext()
            {
                if (iter.MoveNext())
                {
                    m_current = iter.Current;
                    return true;
                }
                m_current = null;
                return false;
            }
        }

        /// <summary>
        /// Creates a new <see cref="TermsFilter"/> from the given <see cref="BytesRef"/> array for
        /// a single field.
        /// </summary>
        public TermsFilter(string field, params BytesRef[] terms)
            : this(field, (IList<BytesRef>)terms)
        {
            // this ctor prevents unnecessary Term creations
        }

        /// <summary>
        /// Creates a new <see cref="TermsFilter"/> from the given array. The array can
        /// contain duplicate terms and multiple fields.
        /// </summary>
        public TermsFilter(params Term[] terms)
            : this((IList<Term>)terms)
        {
        }


        private TermsFilter(FieldAndTermEnum iter, int length)
        {
            // TODO: maybe use oal.index.PrefixCodedTerms instead?
            // If number of terms is more than a few hundred it
            // should be a win

            // TODO: we also pack terms in FieldCache/DocValues
            // ... maybe we can refactor to share that code

            // TODO: yet another option is to build the union of the terms in
            // an automaton an call intersect on the termsenum if the density is high
            
            int hash = 9;
            var serializedTerms = Arrays.Empty<byte>();
            this.offsets = new int[length + 1];
            int lastEndOffset = 0;
            int index = 0;
            var termsAndFields = new JCG.List<TermsAndField>();
            TermsAndField lastTermsAndField = null;
            BytesRef previousTerm = null;
            string previousField = null;
            BytesRef currentTerm;
            string currentField;
            while (iter.MoveNext())
            {
                currentTerm = iter.Current;
                currentField = iter.Field;
                if (currentField is null)
                {
                    throw new ArgumentException("Field must not be null");
                }
                if (previousField != null)
                {
                    // deduplicate
                    if (previousField.Equals(currentField, StringComparison.Ordinal))
                    {
                        if (previousTerm.BytesEquals(currentTerm))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        int _start = lastTermsAndField is null ? 0 : lastTermsAndField.end;
                        lastTermsAndField = new TermsAndField(_start, index, previousField);
                        termsAndFields.Add(lastTermsAndField);
                    }
                }
                hash = PRIME * hash + currentField.GetHashCode();
                hash = PRIME * hash + currentTerm.GetHashCode();
                if (serializedTerms.Length < lastEndOffset + currentTerm.Length)
                {
                    serializedTerms = ArrayUtil.Grow(serializedTerms, lastEndOffset + currentTerm.Length);
                }
                Arrays.Copy(currentTerm.Bytes, currentTerm.Offset, serializedTerms, lastEndOffset, currentTerm.Length);
                offsets[index] = lastEndOffset;
                lastEndOffset += currentTerm.Length;
                index++;
                previousTerm = currentTerm;
                previousField = currentField;
            }
            offsets[index] = lastEndOffset;
            int start = lastTermsAndField is null ? 0 : lastTermsAndField.end;
            lastTermsAndField = new TermsAndField(start, index, previousField);
            termsAndFields.Add(lastTermsAndField);
            this.termsBytes = ArrayUtil.Shrink(serializedTerms, lastEndOffset);
            this.termsAndFields = termsAndFields.ToArray();
            this.hashCode = hash;
        }

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            AtomicReader reader = context.AtomicReader;
            FixedBitSet result = null; // lazy init if needed - no need to create a big bitset ahead of time
            Fields fields = reader.Fields;
            BytesRef spare = new BytesRef(this.termsBytes);
            if (fields is null)
            {
                return result;
            }
            Terms terms; // LUCENENET: IDE0059: Remove unnecessary value assignment
            TermsEnum termsEnum = null;
            DocsEnum docs = null;
            foreach (TermsAndField termsAndField in this.termsAndFields)
            {
                if ((terms = fields.GetTerms(termsAndField.field)) != null)
                {
                    termsEnum = terms.GetEnumerator(termsEnum); // this won't return null
                    for (int i = termsAndField.start; i < termsAndField.end; i++)
                    {
                        spare.Offset = offsets[i];
                        spare.Length = offsets[i + 1] - offsets[i];
                        if (termsEnum.SeekExact(spare))
                        {
                            docs = termsEnum.Docs(acceptDocs, docs, DocsFlags.NONE); // no freq since we don't need them
                            if (result is null)
                            {
                                if (docs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                                {
                                    result = new FixedBitSet(reader.MaxDoc);
                                    // lazy init but don't do it in the hot loop since we could read many docs
                                    result.Set(docs.DocID);
                                }
                            }
                            while (docs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                            {
                                result.Set(docs.DocID);
                            }
                        }
                    }
                }
            }
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if ((obj is null) || (obj.GetType() != this.GetType()))
            {
                return false;
            }

            var test = (TermsFilter)obj;
            // first check the fields before even comparing the bytes
            if (test.hashCode == hashCode && Arrays.Equals(termsAndFields, test.termsAndFields))
            {
                int lastOffset = termsAndFields[termsAndFields.Length - 1].end;
                // compare offsets since we sort they must be identical
                if (ArrayUtil.Equals(offsets, 0, test.offsets, 0, lastOffset + 1))
                {
                    // straight byte comparison since we sort they must be identical
                    return ArrayUtil.Equals(termsBytes, 0, test.termsBytes, 0, offsets[lastOffset]);
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            var spare = new BytesRef(termsBytes);
            bool first = true;
            for (int i = 0; i < termsAndFields.Length; i++)
            {
                TermsAndField current = termsAndFields[i];
                for (int j = current.start; j < current.end; j++)
                {
                    spare.Offset = offsets[j];
                    spare.Length = offsets[j + 1] - offsets[j];
                    if (!first)
                    {
                        builder.Append(' ');
                    }
                    first = false;
                    builder.Append(current.field).Append(':');
                    builder.Append(spare.Utf8ToString());
                }
            }

            return builder.ToString();
        }

        private sealed class TermsAndField
        {
            internal readonly int start;
            internal readonly int end;
            internal readonly string field;


            internal TermsAndField(int start, int end, string field)
                : base()
            {
                this.start = start;
                this.end = end;
                this.field = field;
            }

            public override int GetHashCode()
            {
                const int prime = 31;
                int result = 1;
                result = prime * result + ((field is null) ? 0 : field.GetHashCode());
                result = prime * result + end;
                result = prime * result + start;
                return result;
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }
                if (obj is null)
                {
                    return false;
                }
                if (this.GetType() != obj.GetType())
                {
                    return false;
                }
                var other = (TermsAndField)obj;
                if (field is null)
                {
                    if (other.field != null)
                    {
                        return false;
                    }
                }
                else if (!field.Equals(other.field, StringComparison.Ordinal))
                {
                    return false;
                }
                if (end != other.end)
                {
                    return false;
                }
                if (start != other.start)
                {
                    return false;
                }
                return true;
            }

        }

        private abstract class FieldAndTermEnum
        {
            // LUCENENET specific - removed field and changed Field property to protected set
            protected BytesRef m_current;
            public BytesRef Current => m_current;

            public abstract bool MoveNext();

            protected FieldAndTermEnum() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            {
            }

            protected FieldAndTermEnum(string field) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            {
                this.Field = field;
            }

            public virtual string Field { get; protected set; }
        }
    }
}