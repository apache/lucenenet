using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Search;
using Lucene.Net.Search.Mlt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class LikeThisQueryBuilder : IQueryBuilder
    {
        private static readonly int DEFAULT_MAX_QUERY_TERMS = 20;
        private static readonly int DEFAULT_MIN_TERM_FREQUENCY = 1;
        private static readonly float DEFAULT_PERCENT_TERMS_TO_MATCH = 30;
        private readonly Analyzer analyzer;
        private readonly string[] defaultFieldNames;

        public LikeThisQueryBuilder(Analyzer analyzer, string[] defaultFieldNames)
        {
            this.analyzer = analyzer;
            this.defaultFieldNames = defaultFieldNames;
        }

        public Query GetQuery(XElement e)
        {
            string fieldsList = DOMUtils.GetAttribute(e, "fieldNames", "");
            string[] fields = defaultFieldNames;
            if ((fieldsList != null) && (fieldsList.Trim().Length > 0))
            {
                fields = fieldsList.Trim().Split(',');
                for (int i = 0; i < fields.Length; i++)
                {
                    fields[i] = fields[i].Trim();
                }
            }

            string stopWords = DOMUtils.GetAttribute(e, "stopWords", "");
            ISet<String> stopWordsSet = null;
            if ((stopWords != null) && (fields != null))
            {
                stopWordsSet = new HashSet<String>();
                foreach (string field in fields)
                {
                    try
                    {
                        TokenStream ts = analyzer.TokenStream(field, new StringReader(stopWords));
                        ICharTermAttribute termAtt = ts.AddAttribute<ICharTermAttribute>();
                        ts.Reset();
                        while (ts.IncrementToken())
                        {
                            stopWordsSet.Add(termAtt.ToString());
                        }

                        ts.End();
                        ts.Dispose();
                    }
                    catch (IOException ioe)
                    {
                        throw new ParserException(@"IoException parsing stop words list in " + GetType().Name + @":" + ioe.Message);
                    }
                }
            }

            MoreLikeThisQuery mlt = new MoreLikeThisQuery(DOMUtils.GetText(e), fields, analyzer, fields[0]);
            mlt.MaxQueryTerms = DOMUtils.GetAttribute(e, @"maxQueryTerms", DEFAULT_MAX_QUERY_TERMS);
            mlt.MinTermFrequency = DOMUtils.GetAttribute(e, @"minTermFrequency", DEFAULT_MIN_TERM_FREQUENCY);
            mlt.PercentTermsToMatch = DOMUtils.GetAttribute(e, @"percentTermsToMatch", DEFAULT_PERCENT_TERMS_TO_MATCH) / 100;
            mlt.SetStopWords(stopWordsSet);
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
