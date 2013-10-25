using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class UserInputQueryBuilder : IQueryBuilder
    {
        private QueryParser unSafeParser;
        private Analyzer analyzer;
        private string defaultField;

        public UserInputQueryBuilder(QueryParser parser)
        {
            this.unSafeParser = parser;
        }

        public UserInputQueryBuilder(string defaultField, Analyzer analyzer)
        {
            this.analyzer = analyzer;
            this.defaultField = defaultField;
        }

        public Query GetQuery(XElement e)
        {
            string text = DOMUtils.GetText(e);
            try
            {
                Query q = null;
                if (unSafeParser != null)
                {
                    lock (unSafeParser)
                    {
                        q = unSafeParser.Parse(text);
                    }
                }
                else
                {
                    string fieldName = DOMUtils.GetAttribute(e, "fieldName", defaultField);
                    QueryParser parser = CreateQueryParser(fieldName, analyzer);
                    q = parser.Parse(text);
                }

                q.Boost = DOMUtils.GetAttribute(e, @"boost", 1.0f);
                return q;
            }
            catch (ParseException e1)
            {
                throw new ParserException(e1.Message);
            }
        }

        protected virtual QueryParser CreateQueryParser(string fieldName, Analyzer analyzer)
        {
            return new QueryParser(Lucene.Net.Util.Version.LUCENE_CURRENT, fieldName, analyzer);
        }
    }
}
