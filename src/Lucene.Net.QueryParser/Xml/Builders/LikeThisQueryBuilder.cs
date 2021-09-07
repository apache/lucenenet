using J2N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Queries.Mlt;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.QueryParsers.Xml.Builders
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
    /// Builder for <see cref="MoreLikeThisQuery"/>
    /// </summary>
    public class LikeThisQueryBuilder : IQueryBuilder
    {
        private const int DEFAULT_MAX_QUERY_TERMS = 20;
        private const int DEFAULT_MIN_TERM_FREQUENCY = 1;
        private const float DEFAULT_PERCENT_TERMS_TO_MATCH = 30; //default is a 3rd of selected terms must match

        private readonly Analyzer analyzer;
        private readonly string[] defaultFieldNames;

        public LikeThisQueryBuilder(Analyzer analyzer, string[] defaultFieldNames)
        {
            this.analyzer = analyzer;
            this.defaultFieldNames = defaultFieldNames;
        }

        /// <summary>
        /// (non-Javadoc)
        /// @see org.apache.lucene.xmlparser.QueryObjectBuilder#process(org.w3c.dom.Element)
        /// </summary>
        public virtual Query GetQuery(XmlElement e)
        {
            string fieldsList = e.GetAttribute("fieldNames"); //a comma-delimited list of fields
            string[] fields = defaultFieldNames;
            if ((fieldsList != null) && (fieldsList.Trim().Length > 0))
            {
                fields = fieldsList.Trim().Split(',').TrimEnd();
                //trim the fieldnames
                for (int i = 0; i < fields.Length; i++)
                {
                    fields[i] = fields[i].Trim();
                }
            }

            //Parse any "stopWords" attribute
            //TODO MoreLikeThis needs to ideally have per-field stopWords lists - until then
            //I use all analyzers/fields to generate multi-field compatible stop list
            string stopWords = e.GetAttribute("stopWords");
            ISet<string> stopWordsSet = null;
            if ((stopWords != null) && (fields != null))
            {
                stopWordsSet = new JCG.HashSet<string>();
                foreach (string field in fields)
                {
                    TokenStream ts = null;
                    try
                    {
                        ts = analyzer.GetTokenStream(field, stopWords);
                        ICharTermAttribute termAtt = ts.AddAttribute<ICharTermAttribute>();
                        ts.Reset();
                        while (ts.IncrementToken())
                        {
                            stopWordsSet.Add(termAtt.ToString());
                        }
                        ts.End();
                    }
                    catch (Exception ioe) when (ioe.IsIOException())
                    {
                        throw new ParserException("IOException parsing stop words list in "
                            + GetType().Name + ":" + ioe.Message);
                    }
                    finally
                    {
                        IOUtils.DisposeWhileHandlingException(ts);
                    }
                }
            }

            MoreLikeThisQuery mlt = new MoreLikeThisQuery(DOMUtils.GetText(e), fields, analyzer, fields[0]);
            mlt.MaxQueryTerms = DOMUtils.GetAttribute(e, "maxQueryTerms", DEFAULT_MAX_QUERY_TERMS);
            mlt.MinTermFrequency = DOMUtils.GetAttribute(e, "minTermFrequency", DEFAULT_MIN_TERM_FREQUENCY);
            mlt.PercentTermsToMatch = DOMUtils.GetAttribute(e, "percentTermsToMatch", DEFAULT_PERCENT_TERMS_TO_MATCH) / 100;
            mlt.StopWords = stopWordsSet;
            int minDocFreq = DOMUtils.GetAttribute(e, "minDocFreq", -1);
            if (minDocFreq >= 0)
            {
                mlt.MinDocFreq = minDocFreq;
            }

            mlt.Boost = DOMUtils.GetAttribute(e, "boost", 1.0f);

            return mlt;
        }
    }
}
