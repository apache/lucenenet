using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using BytesRef = Lucene.Net.Util.BytesRef;
using FieldInfos = Lucene.Net.Index.FieldInfos;

namespace Lucene.Net.Codecs.Lucene3x
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

    using IndexInput = Lucene.Net.Store.IndexInput;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// @lucene.experimental </summary>
    /// @deprecated (4.0)
    [Obsolete("(4.0)")]
    internal sealed class TermBuffer
    {
        private string field;
        private Term term; // cached

        private BytesRef bytes = new BytesRef(10);

        // Cannot be -1 since (strangely) we write that
        // fieldNumber into index for first indexed term:
        private int currentFieldNumber = -2;

        private static readonly IComparer<BytesRef> utf8AsUTF16Comparator = BytesRef.UTF8SortedAsUTF16Comparer;

        internal int newSuffixStart; // only valid right after .read is called

        public int CompareTo(TermBuffer other)
        {
            if (field == other.field) // fields are interned
            // (only by PreFlex codec)
            {
                return utf8AsUTF16Comparator.Compare(bytes, other.bytes);
            }
            else
            {
                return field.CompareTo(other.field);
            }
        }

        public void Read(IndexInput input, FieldInfos fieldInfos)
        {
            this.term = null; // invalidate cache
            newSuffixStart = input.ReadVInt();
            int length = input.ReadVInt();
            int totalLength = newSuffixStart + length;
            Debug.Assert(totalLength <= ByteBlockPool.BYTE_BLOCK_SIZE - 2, "termLength=" + totalLength + ",resource=" + input);
            if (bytes.Bytes.Length < totalLength)
            {
                bytes.Grow(totalLength);
            }
            bytes.Length = totalLength;
            input.ReadBytes(bytes.Bytes, newSuffixStart, length);
            int fieldNumber = input.ReadVInt();
            if (fieldNumber != currentFieldNumber)
            {
                currentFieldNumber = fieldNumber;
                // NOTE: too much sneakiness here, seriously this is a negative vint?!
                if (currentFieldNumber == -1)
                {
                    field = "";
                }
                else
                {
                    Debug.Assert(fieldInfos.FieldInfo(currentFieldNumber) != null, currentFieldNumber.ToString());
                    
                    field = StringHelper.Intern(fieldInfos.FieldInfo(currentFieldNumber).Name);
                }
            }
            else
            {
                Debug.Assert(field.Equals(fieldInfos.FieldInfo(fieldNumber).Name), "currentFieldNumber=" + currentFieldNumber + " field=" + field + " vs " + fieldInfos.FieldInfo(fieldNumber) == null ? "null" : fieldInfos.FieldInfo(fieldNumber).Name);
            }
        }

        public void Set(Term term)
        {
            if (term == null)
            {
                Reset();
                return;
            }
            bytes.CopyBytes(term.Bytes);
            field = StringHelper.Intern(term.Field);

            currentFieldNumber = -1;
            this.term = term;
        }

        public void Set(TermBuffer other)
        {
            field = other.field;
            currentFieldNumber = other.currentFieldNumber;
            // dangerous to copy Term over, since the underlying
            // BytesRef could subsequently be modified:
            term = null;
            bytes.CopyBytes(other.bytes);
        }

        public void Reset()
        {
            field = null;
            term = null;
            currentFieldNumber = -1;
        }

        public Term ToTerm()
        {
            if (field == null) // unset
            {
                return null;
            }

            return term ?? (term = new Term(field, BytesRef.DeepCopyOf(bytes)));
        }

        public object Clone()
        {
            TermBuffer clone = null;
            try
            {
                clone = (TermBuffer)base.MemberwiseClone();
            }
            catch (InvalidOperationException e)
            {
            }
            clone.bytes = BytesRef.DeepCopyOf(bytes);
            return clone;
        }
    }
}