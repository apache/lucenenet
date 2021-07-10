using Lucene.Net.Benchmarks.Quality.Utils;
using Lucene.Net.Search;
using System;
using System.IO;

namespace Lucene.Net.Benchmarks.Quality
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
    /// Main entry point for running a quality benchmark.
    /// <para/>
    /// There are two main configurations for running a quality benchmark:
    /// <list type="bullet">
    ///     <item><description>Against existing judgements.</description></item>
    ///     <item><description>For submission (e.g. for a contest).</description></item>
    /// </list>
    /// The first configuration requires a non null <see cref="IJudge"/>.
    /// The second configuration requires a non null <see cref="Utils.SubmissionReport"/>.
    /// </summary>
    public class QualityBenchmark
    {
        /// <summary>Quality Queries that this quality benchmark would execute.</summary>
        protected QualityQuery[] m_qualityQueries;

        /// <summary>Parser for turning QualityQueries into Lucene Queries.</summary>
        protected IQualityQueryParser m_qqParser;

        /// <summary>Index to be searched.</summary>
        protected IndexSearcher m_searcher;

        /// <summary>index field to extract doc name for each search result; used for judging the results.</summary>
        protected string m_docNameField;

        /// <summary>maximal number of queries that this quality benchmark runs. Default: maxint. Useful for debugging.</summary>
        private int maxQueries = int.MaxValue;

        /// <summary>Maximal number of results to collect for each query. Default: 1000.</summary>
        private int maxResults = 1000;

        /// <summary>
        /// Create a <see cref="QualityBenchmark"/>.
        /// </summary>
        /// <param name="qqs">Quality queries to run.</param>
        /// <param name="qqParser">Parser for turning QualityQueries into Lucene Queries.</param>
        /// <param name="searcher">Index to be searched.</param>
        /// <param name="docNameField">
        /// Name of field containing the document name.
        /// This allows to extract the doc name for search results,
        /// and is important for judging the results.
        /// </param>
        public QualityBenchmark(QualityQuery[] qqs, IQualityQueryParser qqParser,
            IndexSearcher searcher, string docNameField)
        {
            this.m_qualityQueries = qqs;
            this.m_qqParser = qqParser;
            this.m_searcher = searcher;
            this.m_docNameField = docNameField;
        }

        /// <summary>
        /// Run the quality benchmark.
        /// </summary>
        /// <param name="judge">
        /// The judge that can tell if a certain result doc is relevant for a certain quality query.
        /// If null, no judgements would be made. Usually null for a submission run.
        /// </param>
        /// <param name="submitRep">Submission report is created if non null.</param>
        /// <param name="qualityLog">If not null, quality run data would be printed for each query.</param>
        /// <returns><see cref="QualityStats"/> of each quality query that was executed.</returns>
        /// <exception cref="Exception">If quality benchmark failed to run.</exception>
        public virtual QualityStats[] Execute(IJudge judge, SubmissionReport submitRep,
                                        TextWriter qualityLog)
        {
            int nQueries = Math.Min(maxQueries, m_qualityQueries.Length);
            QualityStats[] stats = new QualityStats[nQueries];
            for (int i = 0; i < nQueries; i++)
            {
                QualityQuery qq = m_qualityQueries[i];
                // generate query
                Query q = m_qqParser.Parse(qq);
                // search with this query 
                long t1 = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                TopDocs td = m_searcher.Search(q, null, maxResults);
                long searchTime = (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - t1; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                //most likely we either submit or judge, but check both 
                if (judge != null)
                {
                    stats[i] = AnalyzeQueryResults(qq, q, td, judge, qualityLog, searchTime);
                }
                if (submitRep != null)
                {
                    submitRep.Report(qq, td, m_docNameField, m_searcher);
                }
            }
            if (submitRep != null)
            {
                submitRep.Flush();
            }
            return stats;
        }

        /// <summary>Analyze/judge results for a single quality query; optionally log them.</summary>
        private QualityStats AnalyzeQueryResults(QualityQuery qq, Query q, TopDocs td, IJudge judge, TextWriter logger, long searchTime)
        {
            QualityStats stts = new QualityStats(judge.MaxRecall(qq), searchTime);
            ScoreDoc[] sd = td.ScoreDocs;
            // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            long t1 = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // extraction of first doc name we measure also construction of doc name extractor, just in case.
            DocNameExtractor xt = new DocNameExtractor(m_docNameField);
            for (int i = 0; i < sd.Length; i++)
            {
                string docName = xt.DocName(m_searcher, sd[i].Doc);
                long docNameExtractTime = (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - t1; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                t1 = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                bool isRelevant = judge.IsRelevant(docName, qq);
                stts.AddResult(i + 1, isRelevant, docNameExtractTime);
            }
            if (logger != null)
            {
                logger.WriteLine(qq.QueryID + "  -  " + q);
                stts.Log(qq.QueryID + " Stats:", 1, logger, "  ");
            }
            return stts;
        }

        /// <summary>
        /// The maximum number of quality queries to run. Useful at debugging.
        /// </summary>
        public virtual int MaxQueries
        {
            get => maxQueries;
            set => maxQueries = value;
        }

        /// <summary>
        /// The maximum number of results to collect for each quality query.
        /// </summary>
        public virtual int MaxResults
        {
            get => maxResults;
            set => maxResults = value;
        }
    }
}
