using Lucene.Net.Facet.Encoding;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class DocValuesCategoryListIterator : ICategoryListIterator
    {
        private readonly IntDecoder decoder;
        private readonly string field;
        private readonly int hashCode;
        private readonly BytesRef bytes = new BytesRef(32);
        private BinaryDocValues current;

        public DocValuesCategoryListIterator(string field, IntDecoder decoder)
        {
            this.field = field;
            this.decoder = decoder;
            this.hashCode = field.GetHashCode();
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override bool Equals(Object o)
        {
            if (!(o is DocValuesCategoryListIterator))
            {
                return false;
            }

            DocValuesCategoryListIterator other = (DocValuesCategoryListIterator)o;
            if (hashCode != other.hashCode)
            {
                return false;
            }

            return field.Equals(other.field);
        }

        public bool SetNextReader(AtomicReaderContext context)
        {
            current = context.AtomicReader.GetBinaryDocValues(field);
            return current != null;
        }

        public void GetOrdinals(int docID, IntsRef ints)
        {
            current.Get(docID, bytes);
            ints.length = 0;
            if (bytes.length > 0)
            {
                decoder.Decode(bytes, ints);
            }
        }

        public override string ToString()
        {
            return field;
        }
    }
}
