using BenchmarkDotNet.Attributes;
using Lucene.Net.Expressions.Benchmarks.Antlr;
using Lucene.Net.Expressions.JS;

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

    /// <summary>Parsing only: the ANTLR parse tree versus the Parlot syntax tree, without emitting IL.</summary>
    [MemoryDiagnoser]
    public class ParseBenchmarks
    {
        private string text;

        [Params("Small", "Medium", "Large")]
        public string Size { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            text = Expressions.Get(Size);
        }

        [Benchmark(Baseline = true)]
        public object Antlr() => AntlrJavascriptCompiler.Parse(text);

        [Benchmark]
        public object Parlot() => JavascriptExpressionParser.Parse(text);
    }
}
