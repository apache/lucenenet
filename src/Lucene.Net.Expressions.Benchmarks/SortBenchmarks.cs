using BenchmarkDotNet.Attributes;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Expressions.Benchmarks.Antlr;
using Lucene.Net.Expressions.JS;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Expressions.Benchmarks
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
    /// The layer above parsing: a search of an in-memory index sorted by an expression. <c>Precompiled</c>
    /// reuses an expression compiled once, as an application caching its expressions would, so the parser
    /// is not involved. The other two compile the expression for each search, as an application taking the
    /// expression from each request would.
    /// </summary>
    [MemoryDiagnoser]
    public class SortBenchmarks
    {
        private Directory directory;
        private DirectoryReader reader;
        private IndexSearcher searcher;
        private SimpleBindings bindings;
        private Query query;
        private Sort precompiled;
        private string text;

        [Params(1_000, 100_000)]
        public int Documents { get; set; }

        [Params("Medium", "Large")]
        public string Size { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            directory = new RAMDirectory();
            var random = new Random(42);
            using (var writer = new IndexWriter(directory, new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48))))
            {
                string[] words = { "lucene", "search", "expression", "parser", "index", "contents" };
                for (int i = 0; i < Documents; i++)
                {
                    writer.AddDocument(new Document
                    {
                        new TextField("body", words[random.Next(words.Length)] + " contents " + words[random.Next(words.Length)], Field.Store.NO),
                        new NumericDocValuesField("popularity", random.Next(10_000)),
                        new NumericDocValuesField("recency", random.Next(365)),
                        new NumericDocValuesField("views", random.Next(100_000)),
                        new NumericDocValuesField("clicks", random.Next(1_000)),
                        new DoubleDocValuesField("lat", random.NextDouble() * 180 - 90),
                        new DoubleDocValuesField("lon", random.NextDouble() * 360 - 180),
                    });
                }
                writer.ForceMerge(1);
            }

            reader = DirectoryReader.Open(directory);
            searcher = new IndexSearcher(reader);
            query = new TermQuery(new Term("body", "contents"));
            bindings = new SimpleBindings
            {
                new SortField("_score", SortFieldType.SCORE),
                new SortField("popularity", SortFieldType.INT64),
                new SortField("recency", SortFieldType.INT64),
                new SortField("views", SortFieldType.INT64),
                new SortField("clicks", SortFieldType.INT64),
                new SortField("lat", SortFieldType.DOUBLE),
                new SortField("lon", SortFieldType.DOUBLE),
            };

            text = Expressions.Get(Size);
            precompiled = new Sort(JavascriptCompiler.Compile(text).GetSortField(bindings, true));
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            reader.Dispose();
            directory.Dispose();
        }

        [Benchmark]
        public TopDocs Precompiled() => searcher.Search(query, null, 10, precompiled);

        [Benchmark(Baseline = true)]
        public TopDocs CompileAntlr() => searcher.Search(query, null, 10, new Sort(AntlrJavascriptCompiler.Compile(text).GetSortField(bindings, true)));

        [Benchmark]
        public TopDocs CompileParlot() => searcher.Search(query, null, 10, new Sort(JavascriptCompiler.Compile(text).GetSortField(bindings, true)));
    }
}
