using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Sandbox.Queries
{
    public sealed class SlowCollatedStringComparator : FieldComparator<String>
    {
        private readonly String[] values;
        private BinaryDocValues currentDocTerms;
        private readonly string field;
        readonly StringComparer collator;
        private string bottom;
        private readonly BytesRef tempBR = new BytesRef();

        public SlowCollatedStringComparator(int numHits, string field, StringComparer collator)
        {
            values = new string[numHits];
            this.field = field;
            this.collator = collator;
        }

        public override int Compare(int slot1, int slot2)
        {
            string val1 = values[slot1];
            string val2 = values[slot2];
            if (val1 == null)
            {
                if (val2 == null)
                {
                    return 0;
                }

                return -1;
            }
            else if (val2 == null)
            {
                return 1;
            }

            return collator.Compare(val1, val2);
        }

        public override int CompareBottom(int doc)
        {
            currentDocTerms.Get(doc, tempBR);
            string val2 = tempBR.bytes == BinaryDocValues.MISSING ? null : tempBR.Utf8ToString();
            if (bottom == null)
            {
                if (val2 == null)
                {
                    return 0;
                }

                return -1;
            }
            else if (val2 == null)
            {
                return 1;
            }

            return collator.Compare(bottom, val2);
        }

        public override void Copy(int slot, int doc)
        {
            currentDocTerms.Get(doc, tempBR);
            if (tempBR.bytes == BinaryDocValues.MISSING)
            {
                values[slot] = null;
            }
            else
            {
                values[slot] = tempBR.Utf8ToString();
            }
        }

        public override FieldComparator SetNextReader(AtomicReaderContext context)
        {
            currentDocTerms = FieldCache.DEFAULT.GetTerms(context.AtomicReader, field);
            return this;
        }

        public override void SetBottom(int bottom)
        {
            this.bottom = values[bottom];
        }

        public override object Value(int slot)
        {
            return values[slot];
        }

        public override int CompareValues(string first, string second)
        {
            if (first == null)
            {
                if (second == null)
                {
                    return 0;
                }

                return -1;
            }
            else if (second == null)
            {
                return 1;
            }
            else
            {
                return collator.Compare(first, second);
            }
        }

        public override int CompareDocToValue(int doc, string value)
        {
            currentDocTerms.Get(doc, tempBR);
            string docValue;
            if (tempBR.bytes == BinaryDocValues.MISSING)
            {
                docValue = null;
            }
            else
            {
                docValue = tempBR.Utf8ToString();
            }

            return CompareValues(docValue, value);
        }
    }
}
