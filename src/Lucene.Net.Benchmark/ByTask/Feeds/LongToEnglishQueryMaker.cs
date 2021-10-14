using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Benchmarks.ByTask.Tasks;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
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
    /// Creates queries whose content is a spelled-out <see cref="long"/> number 
    /// starting from <c><see cref="long.MinValue"/> + 10</c>.
    /// </summary>
    public class Int64ToEnglishQueryMaker : IQueryMaker
    {
        private long counter = long.MinValue + 10;
        protected QueryParser m_parser;

        //// TODO: we could take param to specify locale...
        //private readonly RuleBasedNumberFormat rnbf = new RuleBasedNumberFormat(Locale.ROOT,
        //                                                                     RuleBasedNumberFormat.SPELLOUT);

        public virtual Query MakeQuery(int size)
        {
            throw UnsupportedOperationException.Create();
        }

        public virtual Query MakeQuery()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                //return parser.Parse("" + rnbf.format(GetNextCounter()) + "");
                return m_parser.Parse(GetNextCounter().ToWords());
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private long GetNextCounter()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (counter == long.MaxValue)
                {
                    counter = long.MinValue + 10;
                }
                return counter++;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public virtual void SetConfig(Config config)
        {
            Analyzer anlzr = NewAnalyzerTask.CreateAnalyzer(config.Get("analyzer", typeof(StandardAnalyzer).Name));
            m_parser = new QueryParser(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
                DocMaker.BODY_FIELD, anlzr);
        }

        public virtual void ResetInputs()
        {
            counter = long.MinValue + 10;
        }

        public virtual string PrintQueries()
        {
            return "LongToEnglish: [" + long.MinValue + " TO " + counter + "]";
        }
    }
}
