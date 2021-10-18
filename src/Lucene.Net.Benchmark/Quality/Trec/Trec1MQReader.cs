using J2N.Collections.Generic.Extensions;
using System;
using System.Collections.Generic;
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
    /// Read topics of TREC 1MQ track.
    /// <para/>
    /// Expects this topic format -
    /// <code>
    ///     qnum:qtext
    /// </code>
    /// Comment lines starting with '#' are ignored.
    /// <para/>
    /// All topics will have a single name value pair.
    /// </summary>
    public class Trec1MQReader
    {
        private readonly string name; // LUCENENET: marked readonly

        /// <summary>
        /// Constructor for Trec's 1MQ TopicsReader
        /// </summary>
        /// <param name="name">Name of name-value pair to set for all queries.</param>
        public Trec1MQReader(string name)
            : base()
        {
            this.name = name;
        }

        /// <summary>
        /// Read quality queries from trec 1MQ format topics file.
        /// </summary>
        /// <param name="reader">where queries are read from.</param>
        /// <returns>the result quality queries.</returns>
        /// <exception cref="IOException">if cannot read the queries.</exception>
        public virtual QualityQuery[] ReadQueries(TextReader reader)
        {
            IList<QualityQuery> res = new JCG.List<QualityQuery>();
            string line;
            try
            {
                while (null != (line = reader.ReadLine()))
                {
                    line = line.Trim();
                    if (line.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    // id
                    int k = line.IndexOf(':');
                    string id = line.Substring(0, k - 0).Trim();
                    // qtext
                    string qtext = line.Substring(k + 1).Trim();
                    // we got a topic!
                    IDictionary<string, string> fields = new Dictionary<string, string>
                    {
                        [name] = qtext
                    };
                    //System.out.println("id: "+id+" qtext: "+qtext+"  line: "+line);
                    QualityQuery topic = new QualityQuery(id, fields);
                    res.Add(topic);
                }
            }
            finally
            {
                reader.Dispose();
            }
            // sort result array (by ID)
            QualityQuery[] qq = res.ToArray();
            Array.Sort(qq);
            return qq;
        }
    }
}
