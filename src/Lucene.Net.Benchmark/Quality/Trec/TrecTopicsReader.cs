using J2N.Collections.Generic.Extensions;
using J2N.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    /// Read TREC topics.
    /// </summary>
    /// <remarks>
    /// Expects this topic format -
    /// <code>
    ///   &lt;top&gt;
    ///   &lt;num&gt; Number: nnn
    ///     
    ///   &lt;title&gt; title of the topic
    ///
    ///   &lt;desc&gt; Description:
    ///   description of the topic
    ///
    ///   &lt;narr&gt; Narrative:
    ///   "story" composed by assessors.
    ///
    ///   &lt;/top&gt;
    /// </code>
    /// Comment lines starting with '#' are ignored.
    /// </remarks>
    public class TrecTopicsReader
    {
        private static readonly string newline = Environment.NewLine;

        /// <summary>
        /// Constructor for Trec's TopicsReader
        /// </summary>
        public TrecTopicsReader()
            : base()
        {
        }

        /// <summary>
        /// Read quality queries from trec format topics file.
        /// </summary>
        /// <param name="reader">where queries are read from.</param>
        /// <returns>the result quality queries.</returns>
        /// <exception cref="IOException">if cannot read the queries.</exception>
        public virtual QualityQuery[] ReadQueries(TextReader reader)
        {
            IList<QualityQuery> res = new JCG.List<QualityQuery>();
            StringBuilder sb;
            try
            {
                while (null != (sb = Read(reader, "<top>", null, false, false)))
                {
                    IDictionary<string, string> fields = new Dictionary<string, string>();
                    // id
                    sb = Read(reader, "<num>", null, true, false);
                    int k = sb.IndexOf(":", StringComparison.Ordinal);
                    string id = sb.ToString(k + 1, sb.Length - (k + 1)).Trim();
                    // title
                    sb = Read(reader, "<title>", null, true, false);
                    k = sb.IndexOf(">", StringComparison.Ordinal);
                    string title = sb.ToString(k + 1, sb.Length - (k + 1)).Trim();
                    // description
                    Read(reader, "<desc>", null, false, false);
                    sb.Length = 0;
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("<narr>", StringComparison.Ordinal))
                            break;
                        if (sb.Length > 0) sb.Append(' ');
                        sb.Append(line);
                    }
                    string description = sb.ToString().Trim();
                    // narrative
                    sb.Length = 0;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("</top>", StringComparison.Ordinal))
                            break;
                        if (sb.Length > 0) sb.Append(' ');
                        sb.Append(line);
                    }
                    string narrative = sb.ToString().Trim();
                    // we got a topic!
                    fields["title"] = title;
                    fields["description"] = description;
                    fields["narrative"] = narrative;
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

        // read until finding a line that starts with the specified prefix
        private static StringBuilder Read(TextReader reader, string prefix, StringBuilder sb, bool collectMatchLine, bool collectAll) // LUCENENET: CA1822: Mark members as static
        {
            sb = sb ?? new StringBuilder();
            string sep = "";
            while (true)
            {
                string line = reader.ReadLine();
                if (line is null)
                {
                    return null;
                }
                if (line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    if (collectMatchLine)
                    {
                        sb.Append(sep + line);
                        //sep = newline; // LUCENENET: IDE0059: Remove unnecessary value assignment - this skips out of the loop
                    }
                    break;
                }
                if (collectAll)
                {
                    sb.Append(sep + line);
                    sep = newline;
                }
            }
            //System.out.println("read: "+sb);
            return sb;
        }
    }
}
