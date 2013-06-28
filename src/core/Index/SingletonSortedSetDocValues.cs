using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public class SingletonSortedSetDocValues : SortedSetDocValues
    {
        private readonly SortedDocValues in_renamed;
        private int docID;
        private bool set;

        public SingletonSortedSetDocValues(SortedDocValues in_renamed)
        {
            this.in_renamed = in_renamed;
            //assert NO_MORE_ORDS == -1; // this allows our nextOrd() to work for missing values without a check
        }

        public override long NextOrd()
        {
            if (set)
            {
                return NO_MORE_ORDS;
            }
            else
            {
                set = true;
                return in_renamed.GetOrd(docID);
            }
        }

        public override void SetDocument(int docID)
        {
            this.docID = docID;
            set = false;
        }

        public override void LookupOrd(long ord, BytesRef result)
        {
            // cast is ok: single-valued cannot exceed Integer.MAX_VALUE
            in_renamed.LookupOrd((int)ord, result);
        }

        public override long ValueCount
        {
            get { return in_renamed.ValueCount; }
        }

        public override long LookupTerm(BytesRef key)
        {
            return in_renamed.LookupTerm(key);
        }
    }
}
