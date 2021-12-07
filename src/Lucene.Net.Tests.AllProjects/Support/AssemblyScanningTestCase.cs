using J2N.Text;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Reflection;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Support
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

    public abstract class AssemblyScanningTestCase : LuceneTestCase
    {
        // Load exception types from all assemblies
        public static readonly Assembly[] LuceneAssemblies = new Assembly[]
        {
            typeof(Lucene.Net.Analysis.Analyzer).Assembly,                         // Lucene.Net
            typeof(Lucene.Net.Analysis.Standard.ClassicAnalyzer).Assembly,         // Lucene.Net.Analysis.Common
            typeof(Lucene.Net.Analysis.Ja.GraphvizFormatter).Assembly,             // Lucene.Net.Analysis.Kuromoji
            typeof(Lucene.Net.Analysis.Morfologik.MorfologikAnalyzer).Assembly,    // Lucene.Net.Analysis.Morfologik
#if FEATURE_OPENNLP
            typeof(Lucene.Net.Analysis.OpenNlp.OpenNLPTokenizer).Assembly,         // Lucene.Net.Analysis.OpenNLP
#endif
            typeof(Lucene.Net.Analysis.Phonetic.BeiderMorseFilter).Assembly,       // Lucene.Net.Analysis.Phonetic
            typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile).Assembly,         // Lucene.Net.Analysis.SmartCn
            typeof(Lucene.Net.Analysis.Stempel.StempelFilter).Assembly,            // Lucene.Net.Analysis.Stempel
            typeof(Lucene.Net.Benchmarks.Constants).Assembly,                      // Lucene.Net.Benchmark
            typeof(Lucene.Net.Classification.KNearestNeighborClassifier).Assembly, // Lucene.Net.Classification
            typeof(Lucene.Net.Codecs.BlockTerms.BlockTermsReader).Assembly,        // Lucene.Net.Codecs
            typeof(Lucene.Net.Expressions.Bindings).Assembly,                      // Lucene.Net.Expressions
            typeof(Lucene.Net.Facet.Facets).Assembly,                              // Lucene.Net.Facet
            typeof(Lucene.Net.Search.Grouping.ICollectedSearchGroup).Assembly,     // Lucene.Net.Grouping
            typeof(Lucene.Net.Search.Highlight.DefaultEncoder).Assembly,           // Lucene.Net.Highlighter
            typeof(Lucene.Net.Analysis.Icu.ICUFoldingFilter).Assembly,             // Lucene.Net.ICU
            typeof(Lucene.Net.Search.Join.JoinUtil).Assembly,                      // Lucene.Net.Join
            typeof(Lucene.Net.Index.Memory.MemoryIndex).Assembly,                  // Lucene.Net.Memory
            typeof(Lucene.Net.Misc.SweetSpotSimilarity).Assembly,                  // Lucene.Net.Misc
            typeof(Lucene.Net.Queries.BooleanFilter).Assembly,                     // Lucene.Net.Queries
            typeof(Lucene.Net.QueryParsers.Classic.QueryParser).Assembly,          // Lucene.Net.QueryParser
            typeof(Lucene.Net.Replicator.IReplicator).Assembly,                    // Lucene.Net.Replicator
            typeof(Lucene.Net.Sandbox.Queries.DuplicateFilter).Assembly,           // Lucene.Net.Sandbox
            typeof(Lucene.Net.Spatial.DisjointSpatialFilter).Assembly,             // Lucene.Net.Spatial
            typeof(Lucene.Net.Util.LuceneTestCase).Assembly,                       // Lucene.Net.TestFramework
        };


        public static readonly Assembly[] DotNetAssemblies = new Assembly[]
        {
            typeof(Exception).Assembly
        };

        public static readonly Assembly[] NUnitAssemblies = new Assembly[]
        {
            typeof(NUnit.Framework.AssertionException).Assembly
        };

        public static ISet<Type> LoadTypesSubclassing(Type baseClass, params Assembly[] assemblies)
        {
            if (baseClass is null)
                throw new ArgumentNullException(nameof(baseClass));
            if (assemblies is null)
                throw new ArgumentNullException(nameof(assemblies));

            var result = new JCG.SortedSet<Type>(Comparer<Type>.Create((left, right) => left.FullName.CompareToOrdinal(right.FullName)));
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (baseClass.IsAssignableFrom(type))
                        result.Add(type);
                }
            }
            return result;
        }
    }
}