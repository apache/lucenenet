using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Tests.BenchmarkDotNet.Util;
using Lucene.Net.Util;
using System;
using System.IO;

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
    public class SearchFilesBenchmarks
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.MediumRun);
            }
        }

        private const string QueryString = "settings";
        private static DirectoryInfo indexDirectory;

        [GlobalSetup]
        public void GlobalSetUp()
        {
            var sourceDirectory = PathUtil.CreateTempDir("sourceFiles");

            // Generate content to index (including our string that we will search for)
            int seed = 2342;
            ContentGenerator.GenerateFiles(new Random(seed), sourceDirectory.FullName, 100, QueryString);

            
            // Index the content
            indexDirectory = PathUtil.CreateTempDir("indexFiles");
            IndexFilesBenchmarks.IndexFiles(sourceDirectory, indexDirectory);

            // Cleanup our source files, they are no longer needed
            try
            {
                if (System.IO.Directory.Exists(sourceDirectory.FullName))
                    System.IO.Directory.Delete(sourceDirectory.FullName, recursive: true);
            }
            catch { }
        }

        [GlobalCleanup]
        public void GlobalTearDown()
        {
            try
            {
                if (System.IO.Directory.Exists(indexDirectory.FullName))
                    System.IO.Directory.Delete(indexDirectory.FullName, recursive: true);
            }
            catch { }
        }

        [Benchmark]
        public void SearchFiles()
        {

            string index = indexDirectory.FullName;
            string field = "contents";
            //string queries = null;
            int repeat = 1000;
            //bool raw = false;
            string queryString = QueryString;
            //int hitsPerPage = 10;

            using (IndexReader reader = DirectoryReader.Open(FSDirectory.Open(index)))
            {
                IndexSearcher searcher = new IndexSearcher(reader);
                // :Post-Release-Update-Version.LUCENE_XY:
                Analyzer analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

                // :Post-Release-Update-Version.LUCENE_XY:
                QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, field, analyzer);

                Query query = parser.Parse(queryString.Trim());
                //Console.WriteLine("Searching for: " + query.ToString(field));

                // repeat & time as benchmark
                {
                    //DateTime start = DateTime.UtcNow;
                    for (int i = 0; i < repeat; i++)
                    {
                        searcher.Search(query, null, 100);
                    }
                    //DateTime end = DateTime.UtcNow;
                    //Console.WriteLine("Time: " + (end - start).TotalMilliseconds + "ms");
                }
            } // Disposes reader
        }
    }
}
