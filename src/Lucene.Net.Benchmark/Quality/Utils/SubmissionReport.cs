using Lucene.Net.Search;
using System;
using System.Globalization;
using System.IO;

namespace Lucene.Net.Benchmarks.Quality.Utils
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
    /// Create a log ready for submission.
    /// Extend this class and override
    /// <see cref="Report(QualityQuery, TopDocs, string, IndexSearcher)"/>
    /// to create different reports. 
    /// </summary>
    public class SubmissionReport
    {
        //private NumberFormat nf;
        private readonly string nf; // LUCENENET: marked readonly
        private readonly TextWriter logger; // LUCENENET: marked readonly
        private readonly string name; // LUCENENET: marked readonly

        /// <summary>
        /// Constructor for <see cref="SubmissionReport"/>.
        /// </summary>
        /// <param name="logger">If <c>null</c>, no submission data is created.</param>
        /// <param name="name">Name of this run.</param>
        public SubmissionReport(TextWriter logger, string name)
        {
            this.logger = logger;
            this.name = name;
            nf = "{0:F4}";
        }

        /// <summary>
        /// Report a search result for a certain quality query.
        /// </summary>
        /// <param name="qq">quality query for which the results are reported.</param>
        /// <param name="td">search results for the query.</param>
        /// <param name="docNameField">stored field used for fetching the result doc name.</param>
        /// <param name="searcher">index access for fetching doc name.</param>
        /// <see cref="IOException">in case of a problem.</see>
        public virtual void Report(QualityQuery qq, TopDocs td, string docNameField, IndexSearcher searcher)
        {
            if (logger is null)
            {
                return;
            }
            ScoreDoc[] sd = td.ScoreDocs;
            string sep = " \t ";
            DocNameExtractor xt = new DocNameExtractor(docNameField);
            for (int i = 0; i < sd.Length; i++)
            {
                string docName = xt.DocName(searcher, sd[i].Doc);
                logger.WriteLine(
                  qq.QueryID + sep +
                  "Q0" + sep +
                  Format(docName, 20) + sep +
                  Format("" + i, 7) + sep +
                  //nf.format(sd[i].score) + sep +
                  string.Format(nf, sd[i].Score, CultureInfo.InvariantCulture) + sep +
                  name
                  );
            }
        }

        public virtual void Flush()
        {
            if (logger != null)
            {
                logger.Flush();
            }
        }

        private const string padd = "                                    ";
        private static string Format(string s, int minLen) // LUCENENET: CA1822: Mark members as static
        {
            s = (s ?? "");
            int n = Math.Max(minLen, s.Length);
            return (s + padd).Substring(0, n - 0);
        }
    }
}
