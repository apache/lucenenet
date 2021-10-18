using J2N.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Benchmarks.ByTask.Tasks
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
    /// Create a new <see cref="Analyzer"/> and set it it in the getRunData() for use by all future tasks.
    /// </summary>
    public class NewAnalyzerTask : PerfTask
    {
        private readonly IList<string> analyzerNames;
        private int current;

        public NewAnalyzerTask(PerfRunData runData)
            : base(runData)
        {
            analyzerNames = new JCG.List<string>();
        }

        public static Analyzer CreateAnalyzer(string className)
        {
            Type clazz = Type.GetType(className);
            try
            {
                // first try to use a ctor with version parameter (needed for many new Analyzers that have no default one anymore
                return (Analyzer)Activator.CreateInstance(clazz,
#pragma warning disable 612, 618
                    LuceneVersion.LUCENE_CURRENT);
#pragma warning restore 612, 618
            }
            catch (Exception nsme) when (nsme.IsNoSuchMethodException())
            {
                // otherwise use default ctor
                return (Analyzer)Activator.CreateInstance(clazz);
            }
        }

        public override int DoLogic()
        {
            string analyzerName = null;
            try
            {
                if (current >= analyzerNames.Count)
                {
                    current = 0;
                }
                analyzerName = analyzerNames[current++];
                Analyzer analyzer = null;
                if (null == analyzerName || 0 == analyzerName.Length)
                {
                    analyzerName = typeof(Lucene.Net.Analysis.Standard.StandardAnalyzer).AssemblyQualifiedName;
                }
                // First, lookup analyzerName as a named analyzer factory
                if (RunData.AnalyzerFactories.TryGetValue(analyzerName, out AnalyzerFactory factory) && null != factory)
                {
                    analyzer = factory.Create();
                }
                else
                {
                    if (analyzerName.Contains("."))
                    {
                        if (analyzerName.StartsWith("Standard.", StringComparison.Ordinal))
                        {
                            analyzerName = "Lucene.Net.Analysis." + analyzerName;
                        }
                        analyzer = CreateAnalyzer(analyzerName);
                    }
                    else
                    { // No package
                        try
                        {
                            // Attempt to instantiate a core analyzer
                            string coreClassName = "Lucene.Net.Analysis.Core." + analyzerName;
                            analyzer = CreateAnalyzer(coreClassName);
                            analyzerName = coreClassName;
                        }
                        catch (Exception e) when (e.IsClassNotFoundException())
                        {
                            // If not a core analyzer, try the base analysis package
                            analyzerName = "Lucene.Net.Analysis." + analyzerName;
                            analyzer = CreateAnalyzer(analyzerName);
                        }
                    }
                }
                RunData.Analyzer = analyzer;
            }
            catch (Exception e) when (e.IsException())
            {
                throw RuntimeException.Create("Error creating Analyzer: " + analyzerName, e);
            }
            return 1;
        }

        /// <summary>
        /// Set the params (analyzerName only),  Comma-separate list of Analyzer class names.  If the Analyzer lives in
        /// Lucene.Net.Analysis, the name can be shortened by dropping the Lucene.Net.Analysis part of the Fully Qualified Class Name.
        /// <para/>
        /// Analyzer names may also refer to previously defined AnalyzerFactory's.
        /// <para/>
        /// Example Declaration: 
        /// <code>
        /// {"NewAnalyzer" NewAnalyzer(WhitespaceAnalyzer, SimpleAnalyzer, StopAnalyzer, Standard.StandardAnalyzer) >
        /// </code>
        /// <para/>
        /// Example AnalyzerFactory usage:
        /// <code>
        /// -AnalyzerFactory(name:'whitespace tokenized',WhitespaceTokenizer)
        /// -NewAnalyzer('whitespace tokenized')
        /// </code>
        /// </summary>
        /// <param name="params">analyzerClassName, or empty for the StandardAnalyzer</param>
        public override void SetParams(string @params)
        {

            base.SetParams(@params);
            StreamTokenizer stok = new StreamTokenizer(new StringReader(@params));
            stok.QuoteChar('"');
            stok.QuoteChar('\'');
            stok.EndOfLineIsSignificant = false;
            stok.OrdinaryChar(',');
            try
            {
                while (stok.NextToken() != StreamTokenizer.TokenType_EndOfStream)
                {
                    switch (stok.TokenType)
                    {
                        case ',':
                            {
                                // Do nothing
                                break;
                            }
                        case '\'':
                        case '\"':
                        case StreamTokenizer.TokenType_Word:
                            {
                                analyzerNames.Add(stok.StringValue);
                                break;
                            }
                        default:
                            {
                                throw RuntimeException.Create("Unexpected token: " + stok.ToString());
                            }
                    }
                }
            }
            catch (Exception e) when (e.IsRuntimeException())
            {
                if (e.Message.StartsWith("Line #", StringComparison.Ordinal))
                {
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                }
                else
                {
                    throw RuntimeException.Create("Line #" + (stok.LineNumber + AlgLineNum) + ": ", e);
                }
            }
            catch (Exception t) when (t.IsThrowable())
            {
                throw RuntimeException.Create("Line #" + (stok.LineNumber + AlgLineNum) + ": ", t);
            }
        }

        /// <seealso cref="PerfTask.SupportsParams"/>
        public override bool SupportsParams => true;
    }
}
