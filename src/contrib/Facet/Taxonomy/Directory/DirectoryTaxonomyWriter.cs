using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Facet.Taxonomy.WriterCache;
using Lucene.Net.Facet.Taxonomy.WriterCache.Cl2o;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Facet.Taxonomy.Directory
{
    public class DirectoryTaxonomyWriter : ITaxonomyWriter, IDisposable
    {
        public static readonly string INDEX_EPOCH = "index.epoch";
        private readonly Lucene.Net.Store.Directory dir;
        private readonly IndexWriter indexWriter;
        private readonly ITaxonomyWriterCache cache;
        private int cacheMisses = 0;
        private long indexEpoch;
        private char delimiter = Consts.DEFAULT_DELIMITER;
        private SinglePositionTokenStream parentStream = new SinglePositionTokenStream(Consts.PAYLOAD_PARENT);
        private Field parentStreamField;
        private Field fullPathField;
        private int cacheMissesUntilFill = 11;
        private bool shouldFillCache = true;
        private ReaderManager readerManager;
        private volatile bool initializedReaderManager = false;
        private volatile bool shouldRefreshReaderManager;
        private volatile bool cacheIsComplete;
        private volatile bool isClosed = false;
        private volatile TaxonomyIndexArrays taxoArrays;
        private volatile int nextID;

        private static IDictionary<String, String> ReadCommitData(Lucene.Net.Store.Directory dir)
        {
            SegmentInfos infos = new SegmentInfos();
            infos.Read(dir);
            return infos.UserData;
        }

        public virtual void SetDelimiter(char delimiter)
        {
            EnsureOpen();
            this.delimiter = delimiter;
        }

        public static void Unlock(Lucene.Net.Store.Directory directory)
        {
            IndexWriter.Unlock(directory);
        }

        public DirectoryTaxonomyWriter(Lucene.Net.Store.Directory directory, IndexWriterConfig.OpenMode openMode, 
            ITaxonomyWriterCache cache)
        {
            dir = directory;
            IndexWriterConfig config = CreateIndexWriterConfig(openMode);
            indexWriter = OpenIndexWriter(dir, config);
            openMode = config.OpenModeValue;
            if (!DirectoryReader.IndexExists(directory))
            {
                indexEpoch = 1;
            }
            else
            {
                string epochStr = null;
                IDictionary<String, String> commitData = ReadCommitData(directory);
                if (commitData != null)
                {
                    epochStr = commitData[INDEX_EPOCH];
                }

                indexEpoch = epochStr == null ? 1 : long.Parse(epochStr);
            }

            if (openMode == IndexWriterConfig.OpenMode.CREATE)
            {
                ++indexEpoch;
            }

            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.OmitNorms = true;
            parentStreamField = new Field(Consts.FIELD_PAYLOADS, parentStream, ft);
            fullPathField = new StringField(Consts.FULL, "", Field.Store.YES);
            nextID = indexWriter.MaxDoc;
            if (cache == null)
            {
                cache = DefaultTaxonomyWriterCache();
            }

            this.cache = cache;
            if (nextID == 0)
            {
                cacheIsComplete = true;
                AddCategory(CategoryPath.EMPTY);
            }
            else
            {
                cacheIsComplete = false;
            }
        }

        protected virtual IndexWriter OpenIndexWriter(Lucene.Net.Store.Directory directory, IndexWriterConfig config)
        {
            return new IndexWriter(directory, config);
        }

        protected virtual IndexWriterConfig CreateIndexWriterConfig(IndexWriterConfig.OpenMode openMode)
        {
            return new IndexWriterConfig(Version.LUCENE_43, null).SetOpenMode(openMode).SetMergePolicy(new LogByteSizeMergePolicy());
        }

        private void InitReaderManager()
        {
            if (!initializedReaderManager)
            {
                lock (this)
                {
                    EnsureOpen();
                    if (!initializedReaderManager)
                    {
                        readerManager = new ReaderManager(indexWriter, false);
                        shouldRefreshReaderManager = false;
                        initializedReaderManager = true;
                    }
                }
            }
        }

        public DirectoryTaxonomyWriter(Lucene.Net.Store.Directory directory, IndexWriterConfig.OpenMode openMode)
            : this(directory, openMode, DefaultTaxonomyWriterCache())
        {
        }

        public static ITaxonomyWriterCache DefaultTaxonomyWriterCache()
        {
            return new Cl2oTaxonomyWriterCache(1024, 0.15F, 3);
        }

        public DirectoryTaxonomyWriter(Lucene.Net.Store.Directory d)
            : this(d, IndexWriterConfig.OpenMode.CREATE_OR_APPEND)
        {
        }

        public void Dispose()
        {
            lock (this)
            {
                if (!isClosed)
                {
                    indexWriter.CommitData = CombinedCommitData(indexWriter.CommitData);
                    indexWriter.Commit();
                    DoClose();
                }
            }
        }

        private void DoClose()
        {
            indexWriter.Dispose();
            isClosed = true;
            CloseResources();
        }

        protected virtual void CloseResources()
        {
            lock (this)
            {
                if (initializedReaderManager)
                {
                    readerManager.Dispose();
                    readerManager = null;
                    initializedReaderManager = false;
                }

                if (cache != null)
                {
                    cache.Close();
                }
            }
        }

        protected virtual int FindCategory(CategoryPath categoryPath)
        {
            lock (this)
            {
                int res = cache.Get(categoryPath).GetValueOrDefault();
                if (res >= 0 || cacheIsComplete)
                {
                    return res;
                }

                Interlocked.Increment(ref cacheMisses);
                PerhapsFillCache();
                res = cache.Get(categoryPath).GetValueOrDefault();
                if (res >= 0 || cacheIsComplete)
                {
                    return res;
                }

                InitReaderManager();
                int doc = -1;
                DirectoryReader reader = readerManager.Acquire();
                try
                {
                    BytesRef catTerm = new BytesRef(categoryPath.ToString(delimiter));
                    TermsEnum termsEnum = null;
                    DocsEnum docs = null;
                    foreach (AtomicReaderContext ctx in reader.Leaves)
                    {
                        Terms terms = ctx.AtomicReader.Terms(Consts.FULL);
                        if (terms != null)
                        {
                            termsEnum = terms.Iterator(termsEnum);
                            if (termsEnum.SeekExact(catTerm, true))
                            {
                                docs = termsEnum.Docs(null, docs, 0);
                                doc = docs.NextDoc() + ctx.docBase;
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    readerManager.Release(reader);
                }

                if (doc > 0)
                {
                    AddToCache(categoryPath, doc);
                }

                return doc;
            }
        }

        public int AddCategory(CategoryPath categoryPath)
        {
            EnsureOpen();
            int res = cache.Get(categoryPath).GetValueOrDefault();
            if (res < 0)
            {
                lock (this)
                {
                    res = FindCategory(categoryPath);
                    if (res < 0)
                    {
                        res = InternalAddCategory(categoryPath);
                    }
                }
            }

            return res;
        }

        private int InternalAddCategory(CategoryPath cp)
        {
            int parent;
            if (cp.length > 1)
            {
                CategoryPath parentPath = cp.Subpath(cp.length - 1);
                parent = FindCategory(parentPath);
                if (parent < 0)
                {
                    parent = InternalAddCategory(parentPath);
                }
            }
            else if (cp.length == 1)
            {
                parent = TaxonomyReader.ROOT_ORDINAL;
            }
            else
            {
                parent = TaxonomyReader.INVALID_ORDINAL;
            }

            int id = AddCategoryDocument(cp, parent);
            return id;
        }

        protected void EnsureOpen()
        {
            if (isClosed)
            {
                throw new ObjectDisposedException(@"The taxonomy writer has already been closed");
            }
        }

        private int AddCategoryDocument(CategoryPath categoryPath, int parent)
        {
            parentStream.Set(Math.Max(parent + 1, 1));
            Document d = new Document();
            d.Add(parentStreamField);
            fullPathField.StringValue = categoryPath.ToString(delimiter);
            d.Add(fullPathField);
            indexWriter.AddDocument(d);
            int id = nextID++;
            shouldRefreshReaderManager = true;
            taxoArrays = GetTaxoArrays().Add(id, parent);
            AddToCache(categoryPath, id);
            return id;
        }

        private class SinglePositionTokenStream : TokenStream
        {
            private ICharTermAttribute termAtt;
            private IPositionIncrementAttribute posIncrAtt;
            private bool returned;

            public SinglePositionTokenStream(string word)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
                termAtt.SetEmpty().Append(word);
                returned = true;
            }

            public virtual void Set(int val)
            {
                posIncrAtt.PositionIncrement = val;
                returned = false;
            }

            public override bool IncrementToken()
            {
                if (returned)
                {
                    return false;
                }

                return returned = true;
            }
        }

        private void AddToCache(CategoryPath categoryPath, int id)
        {
            if (cache.Put(categoryPath, id))
            {
                RefreshReaderManager();
                cacheIsComplete = false;
            }
        }

        private void RefreshReaderManager()
        {
            lock (this)
            {
                if (shouldRefreshReaderManager && initializedReaderManager)
                {
                    readerManager.MaybeRefresh();
                    shouldRefreshReaderManager = false;
                }
            }
        }

        public void Commit()
        {
            lock (this)
            {
                EnsureOpen();
                indexWriter.CommitData = CombinedCommitData(indexWriter.CommitData);
                indexWriter.Commit();
            }
        }

        private IDictionary<String, String> CombinedCommitData(IDictionary<String, String> commitData)
        {
            IDictionary<String, String> m = new HashMap<String, String>();
            if (commitData != null)
            {
                foreach (var kvp in commitData)
                {
                    m[kvp.Key] = kvp.Value;
                }
            }

            m[INDEX_EPOCH] = indexEpoch.ToString();
            return m;
        }
        
        public IDictionary<String, String> CommitData
        {
            get
            {
                return CombinedCommitData(indexWriter.CommitData);
            }
            set
            {
                indexWriter.CommitData = CombinedCommitData(value);
            }
        }

        public void PrepareCommit()
        {
            lock (this)
            {
                EnsureOpen();
                indexWriter.CommitData = CombinedCommitData(indexWriter.CommitData);
                indexWriter.PrepareCommit();
            }
        }

        public int Size
        {
            get
            {
                EnsureOpen();
                return nextID;
            }
        }

        public virtual void SetCacheMissesUntilFill(int i)
        {
            EnsureOpen();
            cacheMissesUntilFill = i;
        }

        private void PerhapsFillCache()
        {
            lock (this)
            {
                if (cacheMisses < cacheMissesUntilFill)
                {
                    return;
                }

                if (!shouldFillCache)
                {
                    return;
                }

                shouldFillCache = false;
                InitReaderManager();
                bool aborted = false;
                DirectoryReader reader = readerManager.Acquire();
                try
                {
                    TermsEnum termsEnum = null;
                    DocsEnum docsEnum = null;
                    foreach (AtomicReaderContext ctx in reader.Leaves)
                    {
                        Terms terms = ctx.AtomicReader.Terms(Consts.FULL);
                        if (terms != null)
                        {
                            termsEnum = terms.Iterator(termsEnum);
                            while (termsEnum.Next() != null)
                            {
                                if (!cache.IsFull)
                                {
                                    BytesRef t = termsEnum.Term;
                                    CategoryPath cp = new CategoryPath(t.Utf8ToString(), delimiter);
                                    docsEnum = termsEnum.Docs(null, docsEnum, DocsEnum.FLAG_NONE);
                                    bool res = cache.Put(cp, docsEnum.NextDoc() + ctx.docBase);
                                }
                                else
                                {
                                    aborted = true;
                                    break;
                                }
                            }
                        }

                        if (aborted)
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    readerManager.Release(reader);
                }

                cacheIsComplete = !aborted;
                if (cacheIsComplete)
                {
                    lock (this)
                    {
                        readerManager.Dispose();
                        readerManager = null;
                        initializedReaderManager = false;
                    }
                }
            }
        }

        private TaxonomyIndexArrays GetTaxoArrays()
        {
            if (taxoArrays == null)
            {
                lock (this)
                {
                    if (taxoArrays == null)
                    {
                        InitReaderManager();
                        DirectoryReader reader = readerManager.Acquire();
                        try
                        {
                            TaxonomyIndexArrays tmpArrays = new TaxonomyIndexArrays(reader);
                            taxoArrays = tmpArrays;
                        }
                        finally
                        {
                            readerManager.Release(reader);
                        }
                    }
                }
            }

            return taxoArrays;
        }

        public int GetParent(int ordinal)
        {
            EnsureOpen();
            if (ordinal >= nextID)
            {
                throw new IndexOutOfRangeException(@"requested ordinal is bigger than the largest ordinal in the taxonomy");
            }

            int[] parents = GetTaxoArrays().Parents;
            return parents[ordinal];
        }

        public virtual void AddTaxonomy(Lucene.Net.Store.Directory taxoDir, IOrdinalMap map)
        {
            EnsureOpen();
            DirectoryReader r = DirectoryReader.Open(taxoDir);
            try
            {
                int size = r.NumDocs;
                IOrdinalMap ordinalMap = map;
                ordinalMap.SetSize(size);
                int base_renamed = 0;
                TermsEnum te = null;
                DocsEnum docs = null;
                foreach (AtomicReaderContext ctx in r.Leaves)
                {
                    AtomicReader ar = ctx.AtomicReader;
                    Terms terms = ar.Terms(Consts.FULL);
                    te = terms.Iterator(te);
                    while (te.Next() != null)
                    {
                        string value = te.Term.Utf8ToString();
                        CategoryPath cp = new CategoryPath(value, delimiter);
                        int ordinal = AddCategory(cp);
                        docs = te.Docs(null, docs, DocsEnum.FLAG_NONE);
                        ordinalMap.AddMapping(docs.NextDoc() + base_renamed, ordinal);
                    }

                    base_renamed += ar.MaxDoc;
                }

                ordinalMap.AddDone();
            }
            finally
            {
                r.Dispose();
            }
        }

        public interface IOrdinalMap
        {
            void SetSize(int size);
            void AddMapping(int origOrdinal, int newOrdinal);
            void AddDone();
            int[] GetMap();
        }

        public sealed class MemoryOrdinalMap : IOrdinalMap
        {
            int[] map;
            public void SetSize(int taxonomySize)
            {
                map = new int[taxonomySize];
            }

            public void AddMapping(int origOrdinal, int newOrdinal)
            {
                map[origOrdinal] = newOrdinal;
            }

            public void AddDone()
            {
            }

            public int[] GetMap()
            {
                return map;
            }
        }

        public sealed class DiskOrdinalMap : IOrdinalMap
        {
            FileInfo tmpfile;
            BinaryWriter output; // equivalent to java's DataOutputStream
            FileStream fstream; // .NET Port: need to dispose of this

            public DiskOrdinalMap(FileInfo tmpfile)
            {
                this.tmpfile = tmpfile;
                fstream = tmpfile.OpenWrite();
                output = new BinaryWriter(fstream);
            }

            public void AddMapping(int origOrdinal, int newOrdinal)
            {
                output.Write(origOrdinal);
                output.Write(newOrdinal);
            }

            public void SetSize(int taxonomySize)
            {
                output.Write(taxonomySize);
            }

            public void AddDone()
            {
                if (output != null)
                {
                    output.Dispose();
                    output = null;
                    fstream.Dispose();
                    fstream = null;
                }
            }

            int[] map = null;
            public int[] GetMap()
            {
                if (map != null)
                {
                    return map;
                }

                AddDone();

                using (var fstreamin = tmpfile.OpenRead())
                using (BinaryReader input = new BinaryReader(fstreamin))
                {
                    map = new int[input.ReadInt32()];
                    for (int i = 0; i < map.Length; i++)
                    {
                        int origordinal = input.ReadInt32();
                        int newordinal = input.ReadInt32();
                        map[origordinal] = newordinal;
                    }
                }

                try
                {
                    tmpfile.Delete();
                }
                catch
                {
                }
                //if (!tmpfile.Delete())
                //{
                //    tmpfile.DeleteOnExit();
                //}

                return map;
            }
        }

        public void Rollback()
        {
            lock (this)
            {
                EnsureOpen();
                indexWriter.Rollback();
                DoClose();
            }
        }

        public virtual void ReplaceTaxonomy(Lucene.Net.Store.Directory taxoDir)
        {
            lock (this)
            {
                indexWriter.DeleteAll();
                indexWriter.AddIndexes(taxoDir);
                shouldRefreshReaderManager = true;
                InitReaderManager();
                RefreshReaderManager();
                nextID = indexWriter.MaxDoc;
                cache.Clear();
                cacheIsComplete = false;
                shouldFillCache = true;
                ++indexEpoch;
            }
        }

        public virtual Lucene.Net.Store.Directory Directory
        {
            get
            {
                return dir;
            }
        }

        internal IndexWriter InternalIndexWriter
        {
            get
            {
                return indexWriter;
            }
        }

        public long TaxonomyEpoch
        {
            get
            {
                return indexEpoch;
            }
        }
    }
}
