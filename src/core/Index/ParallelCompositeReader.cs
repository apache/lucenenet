using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public class ParallelCompositeReader : BaseCompositeReader<IndexReader>
    {
        private readonly bool closeSubReaders;
        private readonly ISet<IndexReader> completeReaderSet = new IdentityHashSet<IndexReader>();

        public ParallelCompositeReader(params CompositeReader[] readers)
            : this(true, readers)
        {
        }

        public ParallelCompositeReader(bool closeSubReaders, params CompositeReader[] readers)
            : this(closeSubReaders, readers, readers)
        {
        }

        public ParallelCompositeReader(bool closeSubReaders, CompositeReader[] readers, CompositeReader[] storedFieldReaders)
            : base(PrepareSubReaders(readers, storedFieldReaders))
        {
            this.closeSubReaders = closeSubReaders;
            Collections.AddAll(completeReaderSet, readers);
            Collections.AddAll(completeReaderSet, storedFieldReaders);
            // update ref-counts (like MultiReader):
            if (!closeSubReaders)
            {
                foreach (IndexReader reader in completeReaderSet)
                {
                    reader.IncRef();
                }
            }
            // finally add our own synthetic readers, so we close or decRef them, too (it does not matter what we do)
            completeReaderSet.UnionWith(GetSequentialSubReaders());
        }

        private static IndexReader[] PrepareSubReaders(CompositeReader[] readers, CompositeReader[] storedFieldsReaders)
        {
            if (readers.Length == 0)
            {
                if (storedFieldsReaders.Length > 0)
                    throw new ArgumentException("There must be at least one main reader if storedFieldsReaders are used.");
                return new IndexReader[0];
            }
            else
            {
                IList<IndexReader> firstSubReaders = readers[0].GetSequentialSubReaders();

                // check compatibility:
                int maxDoc = readers[0].MaxDoc, noSubs = firstSubReaders.Count;
                int[] childMaxDoc = new int[noSubs];
                bool[] childAtomic = new bool[noSubs];
                for (int i = 0; i < noSubs; i++)
                {
                    IndexReader r = firstSubReaders[i];
                    childMaxDoc[i] = r.MaxDoc;
                    childAtomic[i] = (r is AtomicReader);
                }
                Validate(readers, maxDoc, childMaxDoc, childAtomic);
                Validate(storedFieldsReaders, maxDoc, childMaxDoc, childAtomic);

                // hierarchically build the same subreader structure as the first CompositeReader with Parallel*Readers:
                IndexReader[] subReaders = new IndexReader[noSubs];
                for (int i = 0; i < subReaders.Length; i++)
                {
                    if (firstSubReaders[i] is AtomicReader)
                    {
                        AtomicReader[] atomicSubs = new AtomicReader[readers.Length];
                        for (int j = 0; j < readers.Length; j++)
                        {
                            atomicSubs[j] = (AtomicReader)readers[j].GetSequentialSubReaders()[i];
                        }
                        AtomicReader[] storedSubs = new AtomicReader[storedFieldsReaders.Length];
                        for (int j = 0; j < storedFieldsReaders.Length; j++)
                        {
                            storedSubs[j] = (AtomicReader)storedFieldsReaders[j].GetSequentialSubReaders()[i];
                        }
                        // We pass true for closeSubs and we prevent closing of subreaders in doClose():
                        // By this the synthetic throw-away readers used here are completely invisible to ref-counting
                        subReaders[i] = new AnonymousParallelAtomicReader(true, atomicSubs, storedSubs);
                    }
                    else
                    {
                        //assert firstSubReaders.get(i) instanceof CompositeReader;
                        CompositeReader[] compositeSubs = new CompositeReader[readers.Length];
                        for (int j = 0; j < readers.Length; j++)
                        {
                            compositeSubs[j] = (CompositeReader)readers[j].GetSequentialSubReaders()[i];
                        }
                        CompositeReader[] storedSubs = new CompositeReader[storedFieldsReaders.Length];
                        for (int j = 0; j < storedFieldsReaders.Length; j++)
                        {
                            storedSubs[j] = (CompositeReader)storedFieldsReaders[j].GetSequentialSubReaders()[i];
                        }
                        // We pass true for closeSubs and we prevent closing of subreaders in doClose():
                        // By this the synthetic throw-away readers used here are completely invisible to ref-counting
                        subReaders[i] = new AnonymousParallelCompositeReader(true, compositeSubs, storedSubs);
                    }
                }
                return subReaders;
            }
        }

        private sealed class AnonymousParallelAtomicReader : ParallelAtomicReader
        {
            public AnonymousParallelAtomicReader(bool closeSubReaders, AtomicReader[] readers, AtomicReader[] storedFieldsReaders)
                : base(closeSubReaders, readers, storedFieldsReaders)
            {
            }

            protected override void DoClose()
            {
            }
        }

        private sealed class AnonymousParallelCompositeReader : ParallelCompositeReader
        {
            public AnonymousParallelCompositeReader(bool closeSubReaders, CompositeReader[] readers, CompositeReader[] storedFieldReaders)
                : base(closeSubReaders, readers, storedFieldReaders)
            {
            }

            protected internal override void DoClose()
            {
            }
        }

        private static void Validate(CompositeReader[] readers, int maxDoc, int[] childMaxDoc, bool[] childAtomic)
        {
            for (int i = 0; i < readers.Length; i++)
            {
                CompositeReader reader = readers[i];
                IList<IndexReader> subs = reader.GetSequentialSubReaders();
                if (reader.MaxDoc != maxDoc)
                {
                    throw new ArgumentException("All readers must have same maxDoc: " + maxDoc + "!=" + reader.MaxDoc);
                }
                int noSubs = subs.Count;
                if (noSubs != childMaxDoc.Length)
                {
                    throw new ArgumentException("All readers must have same number of subReaders");
                }
                for (int subIDX = 0; subIDX < noSubs; subIDX++)
                {
                    IndexReader r = subs[subIDX];
                    if (r.MaxDoc != childMaxDoc[subIDX])
                    {
                        throw new ArgumentException("All readers must have same corresponding subReader maxDoc");
                    }
                    if (!(childAtomic[subIDX] ? (r is AtomicReader) : (r is CompositeReader)))
                    {
                        throw new ArgumentException("All readers must have same corresponding subReader types (atomic or composite)");
                    }
                }
            }
        }

        protected internal override void DoClose()
        {
            System.IO.IOException ioe = null;
            foreach (IndexReader reader in completeReaderSet)
            {
                try
                {
                    if (closeSubReaders)
                    {
                        reader.Dispose();
                    }
                    else
                    {
                        reader.DecRef();
                    }
                }
                catch (System.IO.IOException e)
                {
                    if (ioe == null) ioe = e;
                }
            }
            // throw the first exception
            if (ioe != null) throw ioe;
        }
    }
}
