using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Index
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

    /// <summary>
    /// This tool splits input index into multiple equal parts. The method employed
    /// here uses <see cref="IndexWriter.AddIndexes(IndexReader[])"/> where the input data
    /// comes from the input index with artificially applied deletes to the document
    /// id-s that fall outside the selected partition.
    /// <para>Note 1: Deletes are only applied to a buffered list of deleted docs and
    /// don't affect the source index - this tool works also with read-only indexes.
    /// </para>
    /// <para>Note 2: the disadvantage of this tool is that source index needs to be
    /// read as many times as there are parts to be created, hence the name of this
    /// tool.
    /// 
    /// </para>
    /// <para><b>NOTE</b>: this tool is unaware of documents added
    /// atomically via <see cref="IndexWriter.AddDocuments(IEnumerable{IEnumerable{IIndexableField}}, Analysis.Analyzer)"/> or 
    /// <see cref="IndexWriter.UpdateDocuments(Term, IEnumerable{IEnumerable{IIndexableField}}, Analysis.Analyzer)"/>, which means it can easily
    /// break up such document groups.
    /// </para>
    /// </summary>
    public class MultiPassIndexSplitter
    {

        /// <summary>
        /// Split source index into multiple parts. </summary>
        /// <param name="version">lucene compatibility version</param>
        /// <param name="in"> source index, can have deletions, can have
        /// multiple segments (or multiple readers). </param>
        /// <param name="outputs"> list of directories where the output parts will be stored. </param>
        /// <param name="seq"> if true, then the source index will be split into equal
        /// increasing ranges of document id-s. If false, source document id-s will be
        /// assigned in a deterministic round-robin fashion to one of the output splits. </param>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        public virtual void Split(LuceneVersion version, IndexReader @in, Store.Directory[] outputs, bool seq)
        {
            if (outputs is null || outputs.Length < 2)
            {
                throw new IOException("Invalid number of outputs.");
            }
            if (@in is null || @in.NumDocs < 2)
            {
                throw new IOException("Not enough documents for splitting");
            }
            int numParts = outputs.Length;
            // wrap a potentially read-only input
            // this way we don't have to preserve original deletions because neither
            // deleteDocument(int) or undeleteAll() is applied to the wrapped input index.
            FakeDeleteIndexReader input = new FakeDeleteIndexReader(@in);
            int maxDoc = input.MaxDoc;
            int partLen = maxDoc / numParts;
            for (int i = 0; i < numParts; i++)
            {
                input.UndeleteAll();
                if (seq) // sequential range
                {
                    int lo = partLen * i;
                    int hi = lo + partLen;
                    // below range
                    for (int j = 0; j < lo; j++)
                    {
                        input.DeleteDocument(j);
                    }
                    // above range - last part collects all id-s that remained due to
                    // integer rounding errors
                    if (i < numParts - 1)
                    {
                        for (int j = hi; j < maxDoc; j++)
                        {
                            input.DeleteDocument(j);
                        }
                    }
                }
                else
                {
                    // round-robin
                    for (int j = 0; j < maxDoc; j++)
                    {
                        if ((j + numParts - i) % numParts != 0)
                        {
                            input.DeleteDocument(j);
                        }
                    }
                }
                using IndexWriter w = new IndexWriter(outputs[i],
                    new IndexWriterConfig(version, null) { OpenMode = OpenMode.CREATE });
                Console.Error.WriteLine("Writing part " + (i + 1) + " ...");
                // pass the subreaders directly, as our wrapper's numDocs/hasDeletetions are not up-to-date
                IList<IndexReader> sr = input.GetSequentialSubReaders();
                w.AddIndexes(sr.ToArray()); // TODO: maybe take List<IR> here?
            }
            Console.Error.WriteLine("Done.");
        }

        public static void Main(string[] args)
        {
            if (args.Length < 5)
            {
                // LUCENENET specific - our wrapper console shows the correct usage
                throw new ArgumentException();
                //Console.Error.WriteLine("Usage: MultiPassIndexSplitter -out <outputDir> -num <numParts> [-seq] <inputIndex1> [<inputIndex2 ...]");
                //Console.Error.WriteLine("\tinputIndex\tpath to input index, multiple values are ok");
                //Console.Error.WriteLine("\t-out ouputDir\tpath to output directory to contain partial indexes");
                //Console.Error.WriteLine("\t-num numParts\tnumber of parts to produce");
                //Console.Error.WriteLine("\t-seq\tsequential docid-range split (default is round-robin)");
                //Environment.Exit(-1);
            }
            IList<IndexReader> indexes = new JCG.List<IndexReader>();
            try
            {
                string outDir = null;
                int numParts = -1;
                bool seq = false;
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Equals("-out", StringComparison.Ordinal))
                    {
                        outDir = args[++i];
                    }
                    else if (args[i].Equals("-num", StringComparison.Ordinal))
                    {
                        numParts = Convert.ToInt32(args[++i], CultureInfo.InvariantCulture);
                    }
                    else if (args[i].Equals("-seq", StringComparison.Ordinal))
                    {
                        seq = true;
                    }
                    else
                    {
                        DirectoryInfo file = new DirectoryInfo(args[i]);
                        if (!file.Exists)
                        {
                            Console.Error.WriteLine("Invalid input path - skipping: " + file);
                            continue;
                        }
                        using Store.Directory dir = FSDirectory.Open(new DirectoryInfo(args[i]));
                        try
                        {
                            if (!DirectoryReader.IndexExists(dir))
                            {
                                Console.Error.WriteLine("Invalid input index - skipping: " + file);
                                continue;
                            }
                        }
                        catch (Exception e) when (e.IsException())
                        {
                            Console.Error.WriteLine("Invalid input index - skipping: " + file);
                            continue;
                        }
                        indexes.Add(DirectoryReader.Open(dir));
                    }
                }
                if (outDir is null)
                {
                    throw new Exception("Required argument missing: -out outputDir");
                }
                if (numParts < 2)
                {
                    throw new Exception("Invalid value of required argument: -num numParts");
                }
                if (indexes.Count == 0)
                {
                    throw new Exception("No input indexes to process");
                }
                DirectoryInfo @out = new DirectoryInfo(outDir);
                @out.Create();
                if (!new DirectoryInfo(outDir).Exists)
                {
                    throw new Exception("Can't create output directory: " + @out);
                }
                Store.Directory[] dirs = new Store.Directory[numParts];
                try
                {
                    for (int i = 0; i < numParts; i++)
                    {
                        dirs[i] = FSDirectory.Open(new DirectoryInfo(Path.Combine(@out.FullName, "part-" + i)));
                    }
                    MultiPassIndexSplitter splitter = new MultiPassIndexSplitter();
                    IndexReader input;
                    if (indexes.Count == 1)
                    {
                        input = indexes[0];
                    }
                    else
                    {
                        input = new MultiReader(indexes.ToArray());
                    }
#pragma warning disable 612, 618
                    splitter.Split(LuceneVersion.LUCENE_CURRENT, input, dirs, seq);
#pragma warning restore 612, 618
                }
                finally
                {
                    // LUCENENET specific - properly dispose directories to prevent resource leaks
                    IOUtils.Dispose(dirs);
                }
            }
            finally
            {
                // LUCENENET specific - properly dispose index readers to prevent resource leaks
                IOUtils.Dispose(indexes);
            }
        }

        /// <summary>
        /// This class emulates deletions on the underlying index.
        /// </summary>
        private sealed class FakeDeleteIndexReader : BaseCompositeReader<FakeDeleteAtomicIndexReader>
        {

            public FakeDeleteIndexReader(IndexReader reader)
                    : base(InitSubReaders(reader))
            {
            }

            internal static FakeDeleteAtomicIndexReader[] InitSubReaders(IndexReader reader)
            {
                IList<AtomicReaderContext> leaves = reader.Leaves;
                FakeDeleteAtomicIndexReader[] subs = new FakeDeleteAtomicIndexReader[leaves.Count];
                int i = 0;
                foreach (AtomicReaderContext ctx in leaves)
                {
                    subs[i++] = new FakeDeleteAtomicIndexReader(ctx.AtomicReader);
                }
                return subs;
            }

            public void DeleteDocument(int docID)
            {
                int i = ReaderIndex(docID);
                ((FakeDeleteAtomicIndexReader)GetSequentialSubReaders()[i]).DeleteDocument(docID - ReaderBase(i));
            }

            public void UndeleteAll()
            {
                foreach (FakeDeleteAtomicIndexReader r in GetSequentialSubReaders())
                {
                    r.UndeleteAll();
                }
            }

            protected internal override void DoClose()
            {
            }

            // no need to override numDocs/hasDeletions,
            // as we pass the subreaders directly to IW.addIndexes().
        }

        private sealed class FakeDeleteAtomicIndexReader : FilterAtomicReader
        {
            internal FixedBitSet liveDocs;

            public FakeDeleteAtomicIndexReader(AtomicReader reader) : base(reader)
            {
                UndeleteAll(); // initialize main bitset
            }

            public override int NumDocs => liveDocs.Cardinality;

            public void UndeleteAll()
            {
                int maxDoc = m_input.MaxDoc;
                liveDocs = new FixedBitSet(m_input.MaxDoc);
                if (m_input.HasDeletions)
                {
                    IBits oldLiveDocs = m_input.LiveDocs;
                    if (Debugging.AssertsEnabled) Debugging.Assert(oldLiveDocs != null);
                    // this loop is a little bit ineffective, as Bits has no nextSetBit():
                    for (int i = 0; i < maxDoc; i++)
                    {
                        if (oldLiveDocs.Get(i))
                        {
                            liveDocs.Set(i);
                        }
                    }
                }
                else
                {
                    // mark all docs as valid
                    liveDocs.Set(0, maxDoc);
                }
            }

            public void DeleteDocument(int n)
            {
                liveDocs.Clear(n);
            }

            public override IBits LiveDocs => liveDocs;
        }
    }
}