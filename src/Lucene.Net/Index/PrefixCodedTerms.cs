using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
    using IndexInput = Lucene.Net.Store.IndexInput;
    using RAMFile = Lucene.Net.Store.RAMFile;
    using RAMInputStream = Lucene.Net.Store.RAMInputStream;
    using RAMOutputStream = Lucene.Net.Store.RAMOutputStream;

    /// <summary>
    /// Prefix codes term instances (prefixes are shared)
    /// <para/>
    /// @lucene.experimental
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal class PrefixCodedTerms : IEnumerable<Term>
    {
        internal readonly RAMFile buffer;

        private PrefixCodedTerms(RAMFile buffer)
        {
            this.buffer = buffer;
        }

        /// <returns> size in bytes </returns>
        public virtual long GetSizeInBytes()
        {
            return buffer.GetSizeInBytes();
        }

        /// <returns> iterator over the bytes </returns>
        public virtual IEnumerator<Term> GetEnumerator()
        {
            return new PrefixCodedTermsIterator(buffer);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        internal class PrefixCodedTermsIterator : IEnumerator<Term>
        {
            private readonly IndexInput input;
            private string field = "";
            private BytesRef bytes = new BytesRef();
            private Term term;

            internal PrefixCodedTermsIterator(RAMFile buffer)
            {
                term = new Term(field, bytes);

                try
                {
                    input = new RAMInputStream("PrefixCodedTermsIterator", buffer);
                }
                catch (System.IO.IOException e)
                {
                    throw new Exception(e.ToString(), e);
                }
            }

            public virtual Term Current
            {
                get { return term; }
            }

            public virtual void Dispose()
            {
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public virtual bool MoveNext()
            {
                if (input.GetFilePointer() < input.Length)
                {
                    try
                    {
                        int code = input.ReadVInt32();
                        if ((code & 1) != 0)
                        {
                            field = input.ReadString();
                        }
                        int prefix = Number.URShift(code, 1);
                        int suffix = input.ReadVInt32();
                        bytes.Grow(prefix + suffix);
                        input.ReadBytes(bytes.Bytes, prefix, suffix);
                        bytes.Length = prefix + suffix;
                        term.Set(field, bytes);
                    }
                    catch (IOException e)
                    {
                        throw new Exception(e.ToString(), e);
                    }

                    return true;
                }
                return false;
            }

            public virtual void Reset()
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Builds a <see cref="PrefixCodedTerms"/>: call add repeatedly, then finish. </summary>
#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        public class Builder
        {
            public Builder()
            {
                InitializeInstanceFields();
            }

            internal virtual void InitializeInstanceFields()
            {
                output = new RAMOutputStream(buffer);
            }

            private RAMFile buffer = new RAMFile();
            private RAMOutputStream output;
            private Term lastTerm = new Term("");

            /// <summary>
            /// add a term </summary>
            public virtual void Add(Term term)
            {
                Debug.Assert(lastTerm.Equals(new Term("")) || term.CompareTo(lastTerm) > 0);

                try
                {
                    int prefix = SharedPrefix(lastTerm.Bytes, term.Bytes);
                    int suffix = term.Bytes.Length - prefix;
                    if (term.Field.Equals(lastTerm.Field, StringComparison.Ordinal))
                    {
                        output.WriteVInt32(prefix << 1);
                    }
                    else
                    {
                        output.WriteVInt32(prefix << 1 | 1);
                        output.WriteString(term.Field);
                    }
                    output.WriteVInt32(suffix);
                    output.WriteBytes(term.Bytes.Bytes, term.Bytes.Offset + prefix, suffix);
                    lastTerm.Bytes.CopyBytes(term.Bytes);
                    lastTerm.Field = term.Field;
                }
                catch (IOException e)
                {
                    throw new Exception(e.ToString(), e);
                }
            }

            /// <returns>
            /// finalized form </returns>
            public virtual PrefixCodedTerms Finish()
            {
                try
                {
                    output.Dispose();
                    return new PrefixCodedTerms(buffer);
                }
                catch (IOException e)
                {
                    throw new Exception(e.ToString(), e);
                }
            }

            private int SharedPrefix(BytesRef term1, BytesRef term2)
            {
                int pos1 = 0;
                int pos1End = pos1 + Math.Min(term1.Length, term2.Length);
                int pos2 = 0;
                while (pos1 < pos1End)
                {
                    if (term1.Bytes[term1.Offset + pos1] != term2.Bytes[term2.Offset + pos2])
                    {
                        return pos1;
                    }
                    pos1++;
                    pos2++;
                }
                return pos1;
            }
        }
    }
}