// Lucene version compatibility level 4.8.1
using Lucene.Net.Support;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Miscellaneous
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
    /// This analyzer is used to facilitate scenarios where different
    /// fields Require different analysis techniques.  Use the Map
    /// argument in <see cref="PerFieldAnalyzerWrapper(Analyzer, IDictionary{string, Analyzer})"/>
    /// to add non-default analyzers for fields.
    /// 
    /// <para>Example usage:
    /// 
    /// <code>
    /// IDictionary&lt;string, Analyzer&gt; analyzerPerField = new Dictionary&lt;string, Analyzer&gt;();
    /// analyzerPerField["firstname"] = new KeywordAnalyzer();
    /// analyzerPerField["lastname"] = new KeywordAnalyzer();
    /// 
    /// PerFieldAnalyzerWrapper aWrapper =
    ///   new PerFieldAnalyzerWrapper(new StandardAnalyzer(version), analyzerPerField);
    /// </code>
    /// </para>
    /// <para>
    /// In this example, <see cref="Standard.StandardAnalyzer"/> will be used for all fields except "firstname"
    /// and "lastname", for which <see cref="Core.KeywordAnalyzer"/> will be used.
    /// </para>
    /// <para>A <see cref="PerFieldAnalyzerWrapper"/> can be used like any other analyzer, for both indexing
    /// and query parsing.
    /// </para>
    /// </summary>
    public sealed class PerFieldAnalyzerWrapper : AnalyzerWrapper
    {
        private readonly Analyzer defaultAnalyzer;
        private readonly IDictionary<string, Analyzer> fieldAnalyzers;

        /// <summary>
        /// Constructs with default analyzer.
        /// </summary>
        /// <param name="defaultAnalyzer"> Any fields not specifically
        /// defined to use a different analyzer will use the one provided here. </param>
        public PerFieldAnalyzerWrapper(Analyzer defaultAnalyzer)
            : this(defaultAnalyzer, null)
        {
        }

        /// <summary>
        /// Constructs with default analyzer and a map of analyzers to use for 
        /// specific fields.
        /// <para/>
        /// The type of <see cref="IDictionary{TKey, TValue}"/> supplied will determine the type of behavior.
        /// <list type="table">
        ///     <item>
        ///         <term><see cref="Dictionary{TKey, TValue}"/></term>
        ///         <description>General use. <c>null</c> keys are not supported.</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="SortedDictionary{TKey, TValue}"/></term>
        ///         <description>Use when sorted keys are required. <c>null</c> keys are not supported.</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="JCG.Dictionary{TKey, TValue}"/></term>
        ///         <description>Similar behavior as <see cref="Dictionary{TKey, TValue}"/>. <c>null</c> keys are supported.</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="JCG.SortedDictionary{TKey, TValue}"/></term>
        ///         <description>Use when sorted keys are required. <c>null</c> keys are supported.</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="JCG.LinkedDictionary{TKey, TValue}"/></term>
        ///         <description>Use when insertion order must be preserved (<see cref="Dictionary{TKey, TValue}"/> preserves insertion
        ///             order only until items are removed). <c>null</c> keys are supported.</description>
        ///     </item>
        /// </list>
        /// Or, use a 3rd party or custom <see cref="IDictionary{TKey, TValue}"/> if other behavior is desired.
        /// </summary>
        /// <param name="defaultAnalyzer"> Any fields not specifically
        /// defined to use a different analyzer will use the one provided here. </param>
        /// <param name="fieldAnalyzers"> A <see cref="IDictionary{TKey, TValue}"/> (String field name to the Analyzer) to be 
        /// used for those fields. </param>
        public PerFieldAnalyzerWrapper(Analyzer defaultAnalyzer, IDictionary<string, Analyzer> fieldAnalyzers)
            : base(PER_FIELD_REUSE_STRATEGY)
        {
            this.defaultAnalyzer = defaultAnalyzer;
            this.fieldAnalyzers = fieldAnalyzers ?? Collections.EmptyMap<string, Analyzer>(); // LUCENENET-615: Must support nullable keys
        }

        protected override Analyzer GetWrappedAnalyzer(string fieldName)
        {
            fieldAnalyzers.TryGetValue(fieldName, out Analyzer analyzer);
            return analyzer ?? defaultAnalyzer;
        }

        public override string ToString()
        {
            return "PerFieldAnalyzerWrapper(" + fieldAnalyzers + ", default=" + defaultAnalyzer + ")";
        }
    }
}