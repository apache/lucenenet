using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.QueryParsers.Analyzing
{
    public class AnalyzingQueryParser : QueryParser
    {
        public AnalyzingQueryParser(Version matchVersion, string field, Analyzer analyzer)
            : base(matchVersion, field, analyzer)
        {
            base.AnalyzeRangeTerms = true;
        }

        protected override Query GetWildcardQuery(string field, string termStr)
        {
            IList<string> tlist = new List<string>();
            IList<string> wlist = new List<string>();
            /* somewhat a hack: find/store wildcard chars
             * in order to put them back after analyzing */
            bool isWithinToken = (!termStr.StartsWith("?") && !termStr.StartsWith("*"));
            StringBuilder tmpBuffer = new StringBuilder();
            char[] chars = termStr.ToCharArray();
            for (int i = 0; i < termStr.Length; i++)
            {
                if (chars[i] == '?' || chars[i] == '*')
                {
                    if (isWithinToken)
                    {
                        tlist.Add(tmpBuffer.ToString());
                        tmpBuffer.Length = 0;
                    }
                    isWithinToken = false;
                }
                else
                {
                    if (!isWithinToken)
                    {
                        wlist.Add(tmpBuffer.ToString());
                        tmpBuffer.Length = 0;
                    }
                    isWithinToken = true;
                }
                tmpBuffer.Append(chars[i]);
            }
            if (isWithinToken)
            {
                tlist.Add(tmpBuffer.ToString());
            }
            else
            {
                wlist.Add(tmpBuffer.ToString());
            }

            // get Analyzer from superclass and tokenize the term
            TokenStream source;

            int countTokens = 0;
            try
            {
                source = this.Analyzer.TokenStream(field, new StringReader(termStr));
                source.Reset();
            }
            catch (IOException)
            {
                throw;
            }
            ICharTermAttribute termAtt = source.AddAttribute<ICharTermAttribute>();
            while (true)
            {
                try
                {
                    if (!source.IncrementToken()) break;
                }
                catch (IOException)
                {
                    break;
                }
                String term = termAtt.ToString();
                if (!"".Equals(term))
                {
                    try
                    {
                        tlist[countTokens++] = term;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        countTokens = -1;
                    }
                }
            }
            try
            {
                source.End();
                source.Dispose();
            }
            catch (IOException)
            {
                // ignore
            }

            if (countTokens != tlist.Count)
            {
                /* this means that the analyzer used either added or consumed 
                 * (common for a stemmer) tokens, and we can't build a WildcardQuery */
                throw new ParseException("Cannot build WildcardQuery with analyzer "
                    + this.Analyzer.GetType() + " - tokens added or lost");
            }

            if (tlist.Count == 0)
            {
                return null;
            }
            else if (tlist.Count == 1)
            {
                if (wlist != null && wlist.Count == 1)
                {
                    /* if wlist contains one wildcard, it must be at the end, because:
                     * 1) wildcards are not allowed in 1st position of a term by QueryParser
                     * 2) if wildcard was *not* in end, there would be *two* or more tokens */
                    return base.GetWildcardQuery(field, tlist[0] + wlist[0].ToString());
                }
                else
                {
                    /* we should never get here! if so, this method was called
                     * with a termStr containing no wildcard ... */
                    throw new ArgumentException("getWildcardQuery called without wildcard");
                }
            }
            else
            {
                /* the term was tokenized, let's rebuild to one token
                 * with wildcards put back in postion */
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < tlist.Count; i++)
                {
                    sb.Append(tlist[i]);
                    if (wlist != null && wlist.Count > i)
                    {
                        sb.Append(wlist[i]);
                    }
                }
                return base.GetWildcardQuery(field, sb.ToString());
            }
        }

        protected override Query GetPrefixQuery(string field, string termStr)
        {
            // get Analyzer from superclass and tokenize the term
            TokenStream source;
            IList<String> tlist = new List<String>();
            try
            {
                source = this.Analyzer.TokenStream(field, new StringReader(termStr));
                source.Reset();
            }
            catch (IOException)
            {
                throw;
            }
            ICharTermAttribute termAtt = source.AddAttribute<ICharTermAttribute>();
            while (true)
            {
                try
                {
                    if (!source.IncrementToken()) break;
                }
                catch (IOException)
                {
                    break;
                }
                tlist.Add(termAtt.ToString());
            }

            try
            {
                source.End();
                source.Dispose();
            }
            catch (IOException)
            {
                // ignore
            }

            if (tlist.Count == 1)
            {
                return base.GetPrefixQuery(field, tlist[0]);
            }
            else
            {
                /* this means that the analyzer used either added or consumed
                 * (common for a stemmer) tokens, and we can't build a PrefixQuery */
                throw new ParseException("Cannot build PrefixQuery with analyzer "
                    + this.Analyzer.GetType()
                    + (tlist.Count > 1 ? " - token(s) added" : " - token consumed"));
            }
        }

        protected override Query GetFuzzyQuery(string field, string termStr, float minSimilarity)
        {
            // get Analyzer from superclass and tokenize the term
            TokenStream source = null;
            String nextToken = null;
            bool multipleTokens = false;

            try
            {
                source = this.Analyzer.TokenStream(field, new StringReader(termStr));
                ICharTermAttribute termAtt = source.AddAttribute<ICharTermAttribute>();
                source.Reset();
                if (source.IncrementToken())
                {
                    nextToken = termAtt.ToString();
                }
                multipleTokens = source.IncrementToken();
            }
            catch (IOException)
            {
                nextToken = null;
            }

            try
            {
                source.End();
                source.Dispose();
            }
            catch (IOException)
            {
                // ignore
            }

            if (multipleTokens)
            {
                throw new ParseException("Cannot build FuzzyQuery with analyzer " + this.Analyzer.GetType()
                    + " - tokens were added");
            }

            return (nextToken == null) ? null : base.GetFuzzyQuery(field, nextToken, minSimilarity);
        }
    }
}
