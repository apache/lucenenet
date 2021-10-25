using J2N;
using Lucene.Net.Analysis;
using Lucene.Net.Benchmarks.ByTask.Tasks;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

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
    /// Create queries from a <see cref="FileStream"/>.  One per line, pass them through the
    /// QueryParser.  Lines beginning with # are treated as comments.
    /// </summary>
    /// <remarks>
    /// File can be specified as a absolute, relative or resource.
    /// Two properties can be set:
    /// <list type="bullet">
    ///     <item><term>file.query.maker.file</term><description>&lt;Full path to file containing queries&gt;</description></item>
    ///     <item><term>file.query.maker.default.field</term><description>&lt;Name of default field - Default value is "body"&gt;</description></item>
    /// </list>
    /// <para/>
    /// Example:
    /// <code>
    /// file.query.maker.file=c:/myqueries.txt
    /// file.query.maker.default.field=body
    /// </code>
    /// </remarks>
    public class FileBasedQueryMaker : AbstractQueryMaker, IQueryMaker
    {
        protected override Query[] PrepareQueries()
        {
            Analyzer anlzr = NewAnalyzerTask.CreateAnalyzer(m_config.Get("analyzer",
            typeof(Lucene.Net.Analysis.Standard.StandardAnalyzer).AssemblyQualifiedName));
            string defaultField = m_config.Get("file.query.maker.default.field", DocMaker.BODY_FIELD);
            QueryParser qp = new QueryParser(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
                defaultField, anlzr);
            qp.AllowLeadingWildcard = true;

            JCG.List<Query> qq = new JCG.List<Query>();
            string fileName = m_config.Get("file.query.maker.file", null);
            if (fileName != null)
            {
                FileInfo file = new FileInfo(fileName);
                TextReader reader = null;
                // note: we use a decoding reader, so if your queries are screwed up you know
                if (file.Exists)
                {
                    reader = IOUtils.GetDecodingReader(file, Encoding.UTF8);
                }
                else
                {
                    //see if we can find it as a resource
                    Stream asStream = typeof(FileBasedQueryMaker).FindAndGetManifestResourceStream(fileName);
                    if (asStream != null)
                    {
                        reader = IOUtils.GetDecodingReader(asStream, Encoding.UTF8);
                    }
                }
                if (reader != null)
                {
                    try
                    {
                        string line = null;
                        int lineNum = 0;
                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (line.Length != 0 && !line.StartsWith("#", StringComparison.Ordinal))
                            {
                                try
                                {
                                    qq.Add(qp.Parse(line));
                                }
                                catch (Lucene.Net.QueryParsers.Classic.ParseException e)
                                {
                                    Console.Error.WriteLine("Exception: " + e.Message + " occurred while parsing line: " + lineNum + " Text: " + line);
                                }
                            }
                            lineNum++;
                        }
                    }
                    finally
                    {
                        reader.Dispose();
                    }
                }
                else
                {
                    Console.Error.WriteLine("No Reader available for: " + fileName);
                }

            }
            return qq.ToArray();
        }
    }
}
