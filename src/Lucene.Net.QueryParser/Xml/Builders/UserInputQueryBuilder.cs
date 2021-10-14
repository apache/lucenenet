using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System.Xml;

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
    /// <see cref="UserInputQueryBuilder"/> uses 1 of 2 strategies for thread-safe parsing:
    /// 1) Synchronizing access to "Parse" calls on a previously supplied <see cref="QueryParser"/>
    /// or..
    /// 2) creating a new <see cref="QueryParser"/> object for each parse request
    /// </summary>
    public class UserInputQueryBuilder : IQueryBuilder
    {
        private readonly QueryParser unSafeParser; // LUCENENET: marked readonly
        private readonly Analyzer analyzer; // LUCENENET: marked readonly
        private readonly string defaultField; // LUCENENET: marked readonly

        /// <summary>
        /// This constructor has the disadvantage of not being able to change choice of default field name
        /// </summary>
        /// <param name="parser">thread un-safe query parser</param>
        public UserInputQueryBuilder(QueryParser parser)
        {
            this.unSafeParser = parser;
        }

        public UserInputQueryBuilder(string defaultField, Analyzer analyzer)
        {
            this.analyzer = analyzer;
            this.defaultField = defaultField;
        }

        /// <summary>
        /// (non-Javadoc)
        /// @see org.apache.lucene.xmlparser.QueryObjectBuilder#process(org.w3c.dom.Element)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public virtual Query GetQuery(XmlElement e)
        {
            string text = DOMUtils.GetText(e);
            try
            {
                Query q = null;
                if (unSafeParser != null)
                {
                    //synchronize on unsafe parser
                    UninterruptableMonitor.Enter(unSafeParser);
                    try
                    {
                        q = unSafeParser.Parse(text);
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(unSafeParser);
                    }
                }
                else
                {
                    string fieldName = DOMUtils.GetAttribute(e, "fieldName", defaultField);
                    //Create new parser
                    QueryParser parser = CreateQueryParser(fieldName, analyzer);
                    q = parser.Parse(text);
                }
                q.Boost = DOMUtils.GetAttribute(e, "boost", 1.0f);
                return q;
            }
            catch (Lucene.Net.QueryParsers.Classic.ParseException e1) // LUCENENET: Classic QueryParser has its own ParseException that is different than the one in Support
            {
                throw new ParserException(e1.Message, e1);
            }
        }

        /// <summary>
        /// Method to create a <see cref="QueryParser"/> - designed to be overridden
        /// </summary>
        /// <returns><see cref="QueryParser"/></returns>
        protected virtual QueryParser CreateQueryParser(string fieldName, Analyzer analyzer)
        {
#pragma warning disable 612, 618
            return new QueryParser(LuceneVersion.LUCENE_CURRENT, fieldName, analyzer);
#pragma warning restore 612, 618
        }
    }
}
