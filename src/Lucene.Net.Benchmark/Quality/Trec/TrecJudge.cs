using J2N.Text;
using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Benchmarks.Quality.Trec
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
    /// Judge if given document is relevant to given quality query, based on Trec format for judgements.
    /// </summary>
    public class TrecJudge : IJudge
    {
        private readonly IDictionary<string, QRelJudgement> judgements; // LUCENENET: marked readonly

        /// <summary>
        /// Constructor from a reader.
        /// </summary>
        /// <remarks>
        /// Expected input format:
        /// <code>
        ///     qnum  0   doc-name     is-relevant
        /// </code>
        /// Two sample lines:
        /// <code>
        ///     19    0   doc303       1
        ///     19    0   doc7295      0
        /// </code>
        /// </remarks>
        /// <param name="reader">Where judgments are read from.</param>
        /// <exception cref="IOException">If there is a low-level I/O error.</exception>
        public TrecJudge(TextReader reader)
        {
            judgements = new Dictionary<string, QRelJudgement>();
            QRelJudgement curr = null;
            string zero = "0";
            string line;

            try
            {
                while (null != (line = reader.ReadLine()))
                {
                    line = line.Trim();
                    if (line.Length == 0 || '#' == line[0])
                    {
                        continue;
                    }
                    StringTokenizer st = new StringTokenizer(line);
                    st.MoveNext();
                    string queryID = st.Current;
                    st.MoveNext();
                    st.MoveNext();
                    string docName = st.Current;
                    st.MoveNext();
                    bool relevant = !zero.Equals(st.Current, StringComparison.Ordinal);
                    // LUCENENET: don't call st.NextToken() unless the condition fails.
                    if (Debugging.AssertsEnabled) Debugging.Assert(st.RemainingTokens == 0,"wrong format: {0}  next: {1}", line, (st.MoveNext() ? st.Current : ""));
                    if (relevant)
                    { // only keep relevant docs
                        if (curr is null || !curr.queryID.Equals(queryID, StringComparison.Ordinal))
                        {
                            if (!judgements.TryGetValue(queryID, out curr) || curr is null)
                            {
                                curr = new QRelJudgement(queryID);
                                judgements[queryID] = curr;
                            }
                        }
                        curr.AddRelevantDoc(docName);
                    }
                }
            }
            finally
            {
                reader.Dispose();
            }
        }

        // inherit javadocs
        public virtual bool IsRelevant(string docName, QualityQuery query)
        {
            judgements.TryGetValue(query.QueryID, out QRelJudgement qrj);
            return qrj != null && qrj.IsRelevant(docName);
        }

        /// <summary>
        /// Single Judgement of a trec quality query.
        /// </summary>
        private class QRelJudgement
        {
            internal string queryID;
            private readonly IDictionary<string, string> relevantDocs; // LUCENENET: marked readonly

            internal QRelJudgement(string queryID)
            {
                this.queryID = queryID;
                relevantDocs = new JCG.Dictionary<string, string>();
            }

            public virtual void AddRelevantDoc(string docName)
            {
                relevantDocs[docName] = docName;
            }

            internal virtual bool IsRelevant(string docName)
            {
                return relevantDocs.ContainsKey(docName);
            }

            public virtual int MaxRecall => relevantDocs.Count;
        }

        // inherit javadocs
        public virtual bool ValidateData(QualityQuery[] qq, TextWriter logger)
        {
            IDictionary<string, QRelJudgement> missingQueries = new Dictionary<string, QRelJudgement>(judgements);
            IList<string> missingJudgements = new JCG.List<string>();
            for (int i = 0; i < qq.Length; i++)
            {
                string id = qq[i].QueryID;
                if (!missingQueries.Remove(id))
                    missingJudgements.Add(id);
            }
            bool isValid = true;
            if (missingJudgements.Count > 0)
            {
                isValid = false;
                if (logger != null)
                {
                    logger.WriteLine("WARNING: " + missingJudgements.Count + " queries have no judgments! - ");
                    for (int i = 0; i < missingJudgements.Count; i++)
                    {
                        logger.WriteLine("   " + missingJudgements[i]);
                    }
                }
            }
            if (missingQueries.Count > 0)
            {
                isValid = false;
                if (logger != null)
                {
                    logger.WriteLine("WARNING: " + missingQueries.Count + " judgments match no query! - ");
                    foreach (string id in missingQueries.Keys)
                    {
                        logger.WriteLine("   " + id);
                    }
                }
            }
            return isValid;
        }

        // inherit javadocs
        public virtual int MaxRecall(QualityQuery query)
        {
            if (judgements.TryGetValue(query.QueryID, out QRelJudgement qrj) && qrj != null)
            {
                return qrj.MaxRecall;
            }
            return 0;
        }
    }
}
