using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    /// @lucene.experimental 
    /// </summary>
    [Obsolete("(4.0)")]
    internal sealed class TermBuffer // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        private string field;
        private Term term; // cached

        private BytesRef bytes = new BytesRef(10);

        // Cannot be -1 since (strangely) we write that
        // fieldNumber into index for first indexed term:
        private int currentFieldNumber = -2;

        private static readonly IComparer<BytesRef> utf8AsUTF16Comparer = BytesRef.UTF8SortedAsUTF16Comparer;

        internal int newSuffixStart; // only valid right after .read is called

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(TermBuffer other)
        {
            if (field == other.field) // fields are interned
            // (only by PreFlex codec)
            {
                return utf8AsUTF16Comparer.Compare(bytes, other.bytes);
            }
            else
            {
                return field.CompareToOrdinal(other.field);
            }
        }

        public void Read(IndexInput input, FieldInfos fieldInfos)
        {
            this.term = null; // invalidate cache
            newSuffixStart = input.ReadVInt32();
            int length = input.ReadVInt32();
            int totalLength = newSuffixStart + length;
            if (Debugging.AssertsEnabled) Debugging.Assert(totalLength <= ByteBlockPool.BYTE_BLOCK_SIZE - 2,"termLength={0},resource={1}", totalLength, input);
            if (bytes.Bytes.Length < totalLength)
            {
                bytes.Grow(totalLength);
            }
            bytes.Length = totalLength;
            input.ReadBytes(bytes.Bytes, newSuffixStart, length);
            int fieldNumber = input.ReadVInt32();
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
                    if (Debugging.AssertsEnabled) Debugging.Assert(fieldInfos.FieldInfo(currentFieldNumber) != null, "{0}", currentFieldNumber);
                    
                    field = fieldInfos.FieldInfo(currentFieldNumber).Name.Intern();
                }
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(field.Equals(fieldInfos.FieldInfo(fieldNumber).Name, StringComparison.Ordinal), "currentFieldNumber={0} field={1} vs {2}", currentFieldNumber, field, fieldInfos.FieldInfo(fieldNumber)?.Name ?? "null");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(Term term)
        {
            if (term is null)
            {
                Reset();
                return;
            }
            bytes.CopyBytes(term.Bytes);
            field = term.Field.Intern();

            currentFieldNumber = -1;
            this.term = term;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(TermBuffer other)
        {
            field = other.field;
            currentFieldNumber = other.currentFieldNumber;
            // dangerous to copy Term over, since the underlying
            // BytesRef could subsequently be modified:
            term = null;
            bytes.CopyBytes(other.bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            field = null;
            term = null;
            currentFieldNumber = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Term ToTerm()
        {
            if (field is null) // unset
            {
                return null;
            }

            return term ?? (term = new Term(field, BytesRef.DeepCopyOf(bytes)));
        }

        public object Clone()
        {
            // LUCENENET: MemberwiseClone() doesn't throw in .NET
            TermBuffer clone = (TermBuffer)base.MemberwiseClone();
            clone.bytes = BytesRef.DeepCopyOf(bytes);
            return clone;
        }
    }
}