using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal class PrefixCodedTerms : IEnumerable<Term>
    {
        internal readonly RAMFile buffer;

        private PrefixCodedTerms(RAMFile buffer)
        {
            this.buffer = buffer;
        }

        public long SizeInBytes
        {
            get { return buffer.SizeInBytes; }
        }

        public IEnumerator<Term> GetEnumerator()
        {
            return new PrefixCodedTermsEnumerator(buffer);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class PrefixCodedTermsEnumerator : IEnumerator<Term>
        {
            readonly IndexInput input;
            String field = "";
            BytesRef bytes = new BytesRef();
            Term term;

            public PrefixCodedTermsEnumerator(RAMFile buffer)
            {
                term = new Term(field, bytes);

                try
                {
                    input = new RAMInputStream("PrefixCodedTermsIterator", buffer);
                }
                catch (IOException)
                {
                    throw;
                }
            }

            public Term Current
            {
                get { return term; }
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                if (input.FilePointer < input.Length)
                {
                    int code = input.ReadVInt();
                    if ((code & 1) != 0)
                    {
                        // new field
                        field = input.ReadString();
                    }
                    int prefix = Number.URShift(code, 1);
                    int suffix = input.ReadVInt();
                    bytes.Grow(prefix + suffix);
                    input.ReadBytes(bytes.bytes, prefix, suffix);
                    bytes.length = prefix + suffix;
                    term.Set(field, bytes);
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }

        public class Builder
        {
            private RAMFile buffer = new RAMFile();
            private RAMOutputStream output;
            private Term lastTerm = new Term("");

            public Builder()
            {
                output = new RAMOutputStream(buffer);
            }

            public void Add(Term term)
            {
                //assert lastTerm.equals(new Term("")) || term.compareTo(lastTerm) > 0;

                try
                {
                    int prefix = SharedPrefix(lastTerm.Bytes, term.Bytes);
                    int suffix = term.Bytes.length - prefix;
                    if (term.Field.Equals(lastTerm.Field))
                    {
                        output.WriteVInt(prefix << 1);
                    }
                    else
                    {
                        output.WriteVInt(prefix << 1 | 1);
                        output.WriteString(term.Field);
                    }
                    output.WriteVInt(suffix);
                    output.WriteBytes(term.Bytes.bytes, term.Bytes.offset + prefix, suffix);
                    lastTerm.Bytes.CopyBytes(term.Bytes);
                    lastTerm.Field = term.Field;
                }
                catch (IOException)
                {
                    throw;
                }
            }

            public PrefixCodedTerms Finish()
            {
                try
                {
                    output.Dispose();
                    return new PrefixCodedTerms(buffer);
                }
                catch (IOException)
                {
                    throw;
                }
            }

            private int SharedPrefix(BytesRef term1, BytesRef term2)
            {
                int pos1 = 0;
                int pos1End = pos1 + Math.Min(term1.length, term2.length);
                int pos2 = 0;
                while (pos1 < pos1End)
                {
                    if (term1.bytes[term1.offset + pos1] != term2.bytes[term2.offset + pos2])
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
