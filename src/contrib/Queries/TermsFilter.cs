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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lucene.Net.Index;
using Lucene.Net.Util;
using Lucene.Net.Support;

namespace Lucene.Net.Search
{
    /// <summary>
    /// A filter that contains multiple terms.
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
        private readonly sbyte[] termsBytes;
        private readonly TermsAndField[] termsAndFields;
        private readonly int hashCode; // cached hashcode for fast cache lookups
        private const int PRIME = 31;

        public TermsFilter(List<Term> terms)
            : this(new AnonymousTermFieldAndTermEnum(Sort(terms).GetEnumerator()), terms.Count)
        {
        }

        private sealed class AnonymousTermFieldAndTermEnum : FieldAndTermEnum
        {
            private readonly IEnumerator<Term> iter;

            public AnonymousTermFieldAndTermEnum(IEnumerator<Term> iter)
            {
                this.iter = iter;
            }

            public override BytesRef Next()
            {
                if (iter.MoveNext())
                {
                    Term next = iter.Current;
                    field = next.Field;
                    return next.Bytes;
                }
                return null;
            }
        }

        public TermsFilter(string field, List<BytesRef> terms)
            : this(new AnonymousBytesRefFieldAndTermEnum(Sort(terms).GetEnumerator()), terms.Count)
        {
        }

        private sealed class AnonymousBytesRefFieldAndTermEnum : FieldAndTermEnum
        {
            private readonly IEnumerator<BytesRef> iter;

            public AnonymousBytesRefFieldAndTermEnum(IEnumerator<BytesRef> iter)
            {
                this.iter = iter;
            }

            public override BytesRef Next()
            {
                if (iter.MoveNext())
                {
                    return iter.Current;
                }
                return null;
            }
        }

        public TermsFilter(string field, params BytesRef[] terms)
            : this(field, terms.ToList())
        {
            // this ctor prevents unnecessary Term creations
        }

        public TermsFilter(params Term[] terms)
            : this(terms.ToList())
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
            sbyte[] serializedTerms = new sbyte[0];
            this.offsets = new int[length + 1];
            int lastEndOffset = 0;
            int index = 0;
            List<TermsAndField> termsAndFields = new List<TermsAndField>();
            TermsAndField lastTermsAndField = null;
            BytesRef previousTerm = null;
            String previousField = null;
            BytesRef currentTerm;
            String currentField;
            while ((currentTerm = iter.Next()) != null)
            {
                currentField = iter.Field;
                if (currentField == null)
                {
                    throw new ArgumentException("Field must not be null");
                }
                if (previousField != null)
                {
                    // deduplicate
                    if (previousField.Equals(currentField))
                    {
                        if (previousTerm.BytesEquals(currentTerm))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        int start = lastTermsAndField == null ? 0 : lastTermsAndField.end;
                        lastTermsAndField = new TermsAndField(start, index, previousField);
                        termsAndFields.Add(lastTermsAndField);
                    }
                }
                hash = PRIME * hash + currentField.GetHashCode();
                hash = PRIME * hash + currentTerm.GetHashCode();
                if (serializedTerms.Length < lastEndOffset + currentTerm.length)
                {
                    serializedTerms = ArrayUtil.Grow(serializedTerms, lastEndOffset + currentTerm.length);
                }
                Array.Copy(currentTerm.bytes, currentTerm.offset, serializedTerms, lastEndOffset, currentTerm.length);
                offsets[index] = lastEndOffset;
                lastEndOffset += currentTerm.length;
                index++;
                previousTerm = currentTerm;
                previousField = currentField;
            }
            offsets[index] = lastEndOffset;
            int start2 = lastTermsAndField == null ? 0 : lastTermsAndField.end;
            lastTermsAndField = new TermsAndField(start2, index, previousField);
            termsAndFields.Add(lastTermsAndField);
            this.termsBytes = ArrayUtil.Shrink(serializedTerms, lastEndOffset);
            this.termsAndFields = termsAndFields.ToArray();
            this.hashCode = hash;
        }

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            AtomicReader reader = context.AtomicReader;
            FixedBitSet result = null;  // lazy init if needed - no need to create a big bitset ahead of time
            Fields fields = reader.Fields;
            BytesRef spare = new BytesRef(this.termsBytes);
            if (fields == null)
            {
                return result;
            }
            Terms terms = null;
            TermsEnum termsEnum = null;
            DocsEnum docs = null;
            foreach (TermsAndField termsAndField in this.termsAndFields)
            {
                if ((terms = fields.Terms(termsAndField.field)) != null)
                {
                    termsEnum = terms.Iterator(termsEnum); // this won't return null
                    for (int i = termsAndField.start; i < termsAndField.end; i++)
                    {
                        spare.offset = offsets[i];
                        spare.length = offsets[i + 1] - offsets[i];
                        if (termsEnum.SeekExact(spare, false))
                        { // don't use cache since we could pollute the cache here easily
                            docs = termsEnum.Docs(acceptDocs, docs, DocsEnum.FLAG_NONE); // no freq since we don't need them
                            if (result == null)
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
            if ((obj == null) || (obj.GetType() != this.GetType()))
            {
                return false;
            }

            TermsFilter test = (TermsFilter)obj;
            if (test.hashCode == hashCode && this.termsAndFields.Length == test.termsAndFields.Length)
            {
                // first check the fields before even comparing the bytes
                for (int i = 0; i < termsAndFields.Length; i++)
                {
                    TermsAndField current = termsAndFields[i];
                    if (!current.Equals(test.termsAndFields[i]))
                    {
                        return false;
                    }
                }
                // straight byte comparison since we sort they must be identical
                int end = offsets[termsAndFields.Length];
                sbyte[] left = this.termsBytes;
                sbyte[] right = test.termsBytes;
                for (int i = 0; i < end; i++)
                {
                    if (left[i] != right[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            BytesRef spare = new BytesRef(termsBytes);
            bool first = true;
            for (int i = 0; i < termsAndFields.Length; i++)
            {
                TermsAndField current = termsAndFields[i];
                for (int j = current.start; j < current.end; j++)
                {
                    spare.offset = offsets[j];
                    spare.length = offsets[j + 1] - offsets[j];
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
                result = prime * result + ((field == null) ? 0 : field.GetHashCode());
                result = prime * result + end;
                result = prime * result + start;
                return result;
            }

            public override bool Equals(object obj)
            {
                if (this == obj) return true;
                if (obj == null) return false;
                if (GetType() != obj.GetType()) return false;
                TermsAndField other = (TermsAndField)obj;
                if (field == null)
                {
                    if (other.field != null) return false;
                }
                else if (!field.Equals(other.field)) return false;
                if (end != other.end) return false;
                if (start != other.start) return false;
                return true;
            }
        }

        private abstract class FieldAndTermEnum
        {
            protected string field;

            public abstract BytesRef Next();

            public FieldAndTermEnum() { }

            public FieldAndTermEnum(string field) { this.field = field; }

            public string Field
            {
                get
                {
                    return field;
                }
            }
        }

        private static List<T> Sort<T>(List<T> toSort)
            where T : IComparable<T>
        {
            if (toSort.Count == 0)
            {
                throw new ArgumentException("no terms provided");
            }
            Collections.Sort(toSort);
            return toSort;
        }
    }
}
