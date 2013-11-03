using Lucene.Net.Facet.Search;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Util
{
    public class MultiCategoryListIterator : ICategoryListIterator
    {
        private readonly ICategoryListIterator[] iterators;
        private readonly IList<ICategoryListIterator> validIterators;

        public MultiCategoryListIterator(params ICategoryListIterator[] iterators)
        {
            this.iterators = iterators;
            this.validIterators = new List<ICategoryListIterator>();
        }

        public bool SetNextReader(AtomicReaderContext context)
        {
            validIterators.Clear();
            foreach (ICategoryListIterator cli in iterators)
            {
                if (cli.SetNextReader(context))
                {
                    validIterators.Add(cli);
                }
            }

            return validIterators.Count > 0;
        }

        public void GetOrdinals(int docID, IntsRef ints)
        {
            IntsRef tmp = new IntsRef(ints.length);
            foreach (ICategoryListIterator cli in validIterators)
            {
                cli.GetOrdinals(docID, tmp);
                if (ints.ints.Length < ints.length + tmp.length)
                {
                    ints.Grow(ints.length + tmp.length);
                }

                ints.length += tmp.length;
            }
        }
    }
}
