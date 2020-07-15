using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Lucene.Net.Util;
using Lucene.Net.Store;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using System.Diagnostics;

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
    public class HomePageScriptBenchmarks
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                var baseJob = Job.MediumRun;

                AddJob(baseJob.WithNuGet("Lucene.Net.Analysis.Common", "4.8.0-beta00009").WithId("4.8.0-beta00009"));
                AddJob(baseJob.WithNuGet("Lucene.Net.Analysis.Common", "4.8.0-beta00008").WithId("4.8.0-beta00008"));
                AddJob(baseJob.WithNuGet("Lucene.Net.Analysis.Common", "4.8.0-beta00007").WithId("4.8.0-beta00007"));
            }
        }

        private const int _directoryWriterIterations = 10;
        private const int _indexSearchIterations = 25;

        [Benchmark]
        public void HomePageScript()
        {
            // Ensures index backwards compatibility
            var AppLuceneVersion = LuceneVersion.LUCENE_48;

            for (int d = 0; d < _directoryWriterIterations; d++)
            {
                using var dir = new RAMDirectory();

                //create an analyzer to process the text
                var analyzer = new StandardAnalyzer(AppLuceneVersion);

                //create an index writer
                var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
                using var writer = new IndexWriter(dir, indexConfig);

                for (int i = 0; i < _indexSearchIterations; i++)
                {
                    var source = new
                    {
                        Name = $"Kermit{i} the Frog{i}",
                        FavoritePhrase = $"The quick{i} brown{i} fox{i} jumps{i} over{i} the lazy{i} dog{i} "
                    };
                    Document doc = new Document
                    {
                        // StringField indexes but doesn't tokenize
                        new StringField("name", source.Name, Field.Store.YES),
                        new TextField("favoritePhrase", source.FavoritePhrase, Field.Store.YES)
                    };

                    writer.AddDocument(doc);
                    writer.Flush(triggerMerge: false, applyAllDeletes: false);
                }

                for (int i = 0; i < _indexSearchIterations; i++)
                {
                    // search with a phrase
                    var phrase = new MultiPhraseQuery
                    {
                        new Term("favoritePhrase", $"brown{i}"),
                        new Term("favoritePhrase", $"fox{i}")
                    };

                    // re-use the writer to get real-time updates
                    using var reader = writer.GetReader(applyAllDeletes: true);
                    var searcher = new IndexSearcher(reader);
                    var hits = searcher.Search(phrase, 20 /* top 20 */).ScoreDocs;
                    Debug.Assert(hits.Length > 0);
                    foreach (var hit in hits)
                    {
                        var foundDoc = searcher.Doc(hit.Doc);
                        var score = hit.Score;
                        var name = foundDoc.Get("name");
                        var favoritePhrase = foundDoc.Get("favoritePhrase");
                    }
                }
            }
        }

    }
}
