using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene3x
{
    using Lucene.Net.Util;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldInfos = Lucene.Net.Index.FieldInfos;

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
        private string Field;
        private Term Term; // cached

        private BytesRef Bytes = new BytesRef(10);

        // Cannot be -1 since (strangely) we write that
        // fieldNumber into index for first indexed term:
        private int CurrentFieldNumber = -2;

        private static readonly IComparer<BytesRef> Utf8AsUTF16Comparator = BytesRef.UTF8SortedAsUTF16Comparer;

        internal int NewSuffixStart; // only valid right after .read is called

        public int CompareTo(TermBuffer other)
        {
            if (Field == other.Field) // fields are interned
            // (only by PreFlex codec)
            {
                return Utf8AsUTF16Comparator.Compare(Bytes, other.Bytes);
            }
            else
            {
                return Field.CompareTo(other.Field);
            }
        }

        public void Read(IndexInput input, FieldInfos fieldInfos)
        {
            this.Term = null; // invalidate cache
            NewSuffixStart = input.ReadVInt();
            int length = input.ReadVInt();
            int totalLength = NewSuffixStart + length;
            Debug.Assert(totalLength <= ByteBlockPool.BYTE_BLOCK_SIZE - 2, "termLength=" + totalLength + ",resource=" + input);
            if (Bytes.Bytes.Length < totalLength)
            {
                Bytes.Grow(totalLength);
            }
            Bytes.Length = totalLength;
            input.ReadBytes(Bytes.Bytes, NewSuffixStart, length);
            int fieldNumber = input.ReadVInt();
            if (fieldNumber != CurrentFieldNumber)
            {
                CurrentFieldNumber = fieldNumber;
                // NOTE: too much sneakiness here, seriously this is a negative vint?!
                if (CurrentFieldNumber == -1)
                {
                    Field = "";
                }
                else
                {
                    Debug.Assert(fieldInfos.FieldInfo(CurrentFieldNumber) != null, CurrentFieldNumber.ToString());
                    
                    Field = StringHelper.Intern(fieldInfos.FieldInfo(CurrentFieldNumber).Name);
                }
            }
            else
            {
                Debug.Assert(Field.Equals(fieldInfos.FieldInfo(fieldNumber).Name), "currentFieldNumber=" + CurrentFieldNumber + " field=" + Field + " vs " + fieldInfos.FieldInfo(fieldNumber) == null ? "null" : fieldInfos.FieldInfo(fieldNumber).Name);
            }
        }

        public void Set(Term term)
        {
            if (term == null)
            {
                Reset();
                return;
            }
            Bytes.CopyBytes(term.Bytes);
            Field = StringHelper.Intern(term.Field);

            CurrentFieldNumber = -1;
            this.Term = term;
        }

        public void Set(TermBuffer other)
        {
            Field = other.Field;
            CurrentFieldNumber = other.CurrentFieldNumber;
            // dangerous to copy Term over, since the underlying
            // BytesRef could subsequently be modified:
            Term = null;
            Bytes.CopyBytes(other.Bytes);
        }

        public void Reset()
        {
            Field = null;
            Term = null;
            CurrentFieldNumber = -1;
        }

        public Term ToTerm()
        {
            if (Field == null) // unset
            {
                return null;
            }

            return Term ?? (Term = new Term(Field, BytesRef.DeepCopyOf(Bytes)));
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
            clone.Bytes = BytesRef.DeepCopyOf(Bytes);
            return clone;
        }
    }
}