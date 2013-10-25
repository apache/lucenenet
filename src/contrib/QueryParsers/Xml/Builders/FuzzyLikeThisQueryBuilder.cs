using Lucene.Net.Analysis;
using Lucene.Net.Sandbox.Queries;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class FuzzyLikeThisQueryBuilder : IQueryBuilder
    {
        private static readonly int DEFAULT_MAX_NUM_TERMS = 50;
        private static readonly float DEFAULT_MIN_SIMILARITY = SlowFuzzyQuery.defaultMinSimilarity;
        private static readonly int DEFAULT_PREFIX_LENGTH = 1;
        private static readonly bool DEFAULT_IGNORE_TF = false;
        private readonly Analyzer analyzer;

        public FuzzyLikeThisQueryBuilder(Analyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        public Query GetQuery(XElement e)
        {
            var nl = e.Descendants("Field").ToList();
            int maxNumTerms = DOMUtils.GetAttribute(e, "maxNumTerms", DEFAULT_MAX_NUM_TERMS);
            FuzzyLikeThisQuery fbq = new FuzzyLikeThisQuery(maxNumTerms, analyzer);
            fbq.SetIgnoreTF(DOMUtils.GetAttribute(e, @"ignoreTF", DEFAULT_IGNORE_TF));
            for (int i = 0; i < nl.Count; i++)
            {
                XElement fieldElem = (XElement)nl[i];
                float minSimilarity = DOMUtils.GetAttribute(fieldElem, "minSimilarity", DEFAULT_MIN_SIMILARITY);
                int prefixLength = DOMUtils.GetAttribute(fieldElem, "prefixLength", DEFAULT_PREFIX_LENGTH);
                string fieldName = DOMUtils.GetAttributeWithInheritance(fieldElem, "fieldName");
                string value = DOMUtils.GetText(fieldElem);
                fbq.AddTerms(value, fieldName, minSimilarity, prefixLength);
            }

            fbq.Boost = DOMUtils.GetAttribute(e, @"boost", 1.0f);
            return fbq;
        }
    }
}
