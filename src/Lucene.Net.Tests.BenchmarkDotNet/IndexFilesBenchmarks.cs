using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Store;
using Lucene.Net.Tests.BenchmarkDotNet.Util;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Tests.BenchmarkDotNet
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

    [MemoryDiagnoser]
    [Config(typeof(Config))]
    public class IndexFilesBenchmarks
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                var baseJob = Job.MediumRun;

                AddJob(baseJob.WithNuGet("Lucene.Net.Analysis.Common", "4.8.0-beta00010").WithId("4.8.0-beta00010"));
                AddJob(baseJob.WithNuGet("Lucene.Net.Analysis.Common", "4.8.0-beta00009").WithId("4.8.0-beta00009"));
                AddJob(baseJob.WithNuGet("Lucene.Net.Analysis.Common", "4.8.0-beta00008").WithId("4.8.0-beta00008"));
                AddJob(baseJob.WithNuGet("Lucene.Net.Analysis.Common", "4.8.0-beta00007").WithId("4.8.0-beta00007"));
                AddJob(baseJob.WithNuGet("Lucene.Net.Analysis.Common", "4.8.0-beta00006").WithId("4.8.0-beta00006"));
                AddJob(baseJob.WithNuGet("Lucene.Net.Analysis.Common", "4.8.0-beta00005").WithId("4.8.0-beta00005"));
            }
        }

        private static DirectoryInfo sourceDirectory;
        private static DirectoryInfo indexDirectory;

        [GlobalSetup]
        public void GlobalSetUp()
        {
            sourceDirectory = PathUtil.CreateTempDir("sourceFiles");
            int seed = 2342;
            ContentGenerator.GenerateFiles(new Random(seed), sourceDirectory.FullName, 250);
        }

        [GlobalCleanup]
        public void GlobalTearDown()
        {
            try
            {
                if (System.IO.Directory.Exists(sourceDirectory.FullName))
                    System.IO.Directory.Delete(sourceDirectory.FullName, recursive: true);
            }
            catch { }
        }

        [IterationSetup]
        public void IterationSetUp()
        {
            indexDirectory = PathUtil.CreateTempDir("indexFiles");
        }

        [IterationCleanup]
        public void IterationTearDown()
        {
            try
            {
                if (System.IO.Directory.Exists(indexDirectory.FullName))
                    System.IO.Directory.Delete(indexDirectory.FullName, recursive: true);
            }
            catch { }

        }

        /// <summary>Index all text files under a directory.</summary>
        [Benchmark]
        public void IndexFiles() => IndexFiles(sourceDirectory, indexDirectory);

        /// <summary>Index all text files under a directory.</summary>
        public static void IndexFiles(DirectoryInfo sourceDirectory, DirectoryInfo indexDirectory)
        {
            string indexPath = indexDirectory.FullName;
            
            bool create = true;

            Store.Directory dir = FSDirectory.Open(indexPath);
            // :Post-Release-Update-Version.LUCENE_XY:
            Analyzer analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
            IndexWriterConfig iwc = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);

            if (create)
            {
                // Create a new index in the directory, removing any
                // previously indexed documents:
                iwc.OpenMode = OpenMode.CREATE;
            }
            else
            {
                // Add new documents to an existing index:
                iwc.OpenMode = OpenMode.CREATE_OR_APPEND;
            }

            // Optional: for better indexing performance, if you
            // are indexing many documents, increase the RAM
            // buffer.
            //
            // iwc.RAMBufferSizeMB = 256.0;

            using (IndexWriter writer = new IndexWriter(dir, iwc))
            {
                IndexDocs(writer, sourceDirectory);

                // NOTE: if you want to maximize search performance,
                // you can optionally call forceMerge here.  This can be
                // a terribly costly operation, so generally it's only
                // worth it when your index is relatively static (ie
                // you're done adding documents to it):
                //
                // writer.ForceMerge(1);
            }
        }

        /// <summary>
        /// Recurses over files and directories found under the 
        /// given directory and indexes each file.<para/>
        /// 
        /// NOTE: This method indexes one document per input file. 
        /// This is slow. For good throughput, put multiple documents 
        /// into your input file(s).
        /// </summary>
        /// <param name="writer">
        ///     <see cref="IndexWriter"/> to the index where the given 
        ///     file/dir info will be stored
        /// </param>
        /// <param name="directoryInfo">
        ///     The directory to recurse into to find files to index.
        /// </param>
        /// <exception cref="IOException">
        ///     If there is a low-level I/O error.
        /// </exception>
        internal static void IndexDocs(IndexWriter writer, DirectoryInfo directoryInfo)
        {
            foreach (var dirInfo in directoryInfo.GetDirectories())
            {
                IndexDocs(writer, dirInfo);
            }
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                IndexDocs(writer, fileInfo);
            }
        }

        /// <summary>
        /// Indexes the given file using the given writer.<para/>
        /// </summary>
        /// <param name="writer">
        ///     <see cref="IndexWriter"/> to the index where the given 
        ///     file info will be stored.
        /// </param>
        /// <param name="file">
        ///     The file to index.
        /// </param>
        /// <exception cref="IOException">
        ///     If there is a low-level I/O error.
        /// </exception>
        internal static void IndexDocs(IndexWriter writer, FileInfo file)
        {
            using (FileStream fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                // make a new, empty document
                Document doc = new Document();

                // Add the path of the file as a field named "path".  Use a
                // field that is indexed (i.e. searchable), but don't tokenize 
                // the field into separate words and don't index term frequency
                // or positional information:
                Field pathField = new StringField("path", file.FullName, Field.Store.YES);
                doc.Add(pathField);

                // Add the last modified date of the file a field named "modified".
                // Use a LongField that is indexed (i.e. efficiently filterable with
                // NumericRangeFilter).  This indexes to milli-second resolution, which
                // is often too fine.  You could instead create a number based on
                // year/month/day/hour/minutes/seconds, down the resolution you require.
                // For example the long value 2011021714 would mean
                // February 17, 2011, 2-3 PM.
                doc.Add(new Int64Field("modified", file.LastWriteTimeUtc.Ticks, Field.Store.NO));

                // Add the contents of the file to a field named "contents".  Specify a Reader,
                // so that the text of the file is tokenized and indexed, but not stored.
                // Note that FileReader expects the file to be in UTF-8 encoding.
                // If that's not the case searching for special characters will fail.
                doc.Add(new TextField("contents", new StreamReader(fs, Encoding.UTF8)));

                if (writer.Config.OpenMode == OpenMode.CREATE)
                {
                    // New index, so we just add the document (no old document can be there):
                    //Console.WriteLine("adding " + file);
                    writer.AddDocument(doc);
                }
                else
                {
                    // Existing index (an old copy of this document may have been indexed) so 
                    // we use updateDocument instead to replace the old one matching the exact 
                    // path, if present:
                    //Console.WriteLine("updating " + file);
                    writer.UpdateDocument(new Term("path", file.FullName), doc);
                }
            }
        }
    }
}
