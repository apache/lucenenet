using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    internal sealed class TermBuffer : ICloneable
    {
        private String field;
        private Term term;                            // cached

        private BytesRef bytes = new BytesRef(10);

        // Cannot be -1 since (strangely) we write that
        // fieldNumber into index for first indexed term:
        private int currentFieldNumber = -2;

        private static IComparer<BytesRef> utf8AsUTF16Comparator = BytesRef.UTF8SortedAsUTF16Comparer;

        internal int newSuffixStart;                             // only valid right after .read is called

        public int CompareTo(TermBuffer other)
        {
            if (field == other.field)     // fields are interned
                // (only by PreFlex codec)
                return utf8AsUTF16Comparator.Compare(bytes, other.bytes);
            else
                return field.CompareTo(other.field);
        }

        public void Read(IndexInput input, FieldInfos fieldInfos)
        {
            this.term = null;                           // invalidate cache
            newSuffixStart = input.ReadVInt();
            int length = input.ReadVInt();
            int totalLength = newSuffixStart + length;
            if (bytes.bytes.Length < totalLength)
            {
                bytes.Grow(totalLength);
            }
            bytes.length = totalLength;
            input.ReadBytes(bytes.bytes, newSuffixStart, length);
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
                    //assert fieldInfos.fieldInfo(currentFieldNumber) != null : currentFieldNumber;
                    field = string.Intern(fieldInfos.FieldInfo(currentFieldNumber).name);
                }
            }
            else
            {
                //assert field.equals(fieldInfos.fieldInfo(fieldNumber).name) : "currentFieldNumber=" + currentFieldNumber + " field=" + field + " vs " + fieldInfos.fieldInfo(fieldNumber) == null ? "null" : fieldInfos.fieldInfo(fieldNumber).name;
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
            field = string.Intern(term.Field);
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
            if (field == null)                            // unset
                return null;

            if (term == null)
            {
                term = new Term(field, BytesRef.DeepCopyOf(bytes));
            }

            return term;
        }

        public object Clone()
        {
            TermBuffer clone = null;
            try
            {
                clone = (TermBuffer)base.MemberwiseClone();
            }
            catch { }
            clone.bytes = BytesRef.DeepCopyOf(bytes);
            return clone;
        }
    }
}
