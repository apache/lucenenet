using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.QueryParsers.Analyzing
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

    // LUCENENET: Special case exception - QueryParser has its own ParseException types that are generated.
    // We won't know until we start generating QueryParser how to handle this scenario, but for now we are
    // mapping this explicitly INSIDE of the namespace declaration to prevent our Lucene.ParseException from being
    // used instead.
    using ParseException = Lucene.Net.QueryParsers.Classic.ParseException;

    /// <summary>
    /// Overrides Lucene's default <see cref="QueryParser"/> so that Fuzzy-, Prefix-, Range-, and WildcardQuerys
    /// are also passed through the given analyzer, but wildcard characters <c>*</c> and
    /// <c>?</c> don't get removed from the search terms.
    /// <para/>
    /// <b>Warning:</b> This class should only be used with analyzers that do not use stopwords
    /// or that add tokens. Also, several stemming analyzers are inappropriate: for example, <see cref="Analysis.De.GermanAnalyzer"/>  
    /// will turn <c>Häuser</c> into <c>hau</c>, but <c>H?user</c> will 
    /// become <c>h?user</c> when using this parser and thus no match would be found (i.e.
    /// using this parser will be no improvement over QueryParser in such cases). 
    /// </summary>
    public class AnalyzingQueryParser : Classic.QueryParser
    {
        // gobble escaped chars or find a wildcard character 
        private readonly Regex wildcardPattern = new Regex(@"(\\.)|([?*]+)", RegexOptions.Compiled);

        public AnalyzingQueryParser(LuceneVersion matchVersion, string field, Analyzer analyzer)
            : base(matchVersion, field, analyzer)
        {
            AnalyzeRangeTerms = true;
        }

        /// <summary>
        /// Called when parser parses an input term
        /// that uses prefix notation; that is, contains a single '*' wildcard
        /// character as its last character. Since this is a special case
        /// of generic wildcard term, and such a query can be optimized easily,
        /// this usually results in a different query object.
        /// <para/>
        /// Depending on analyzer and settings, a prefix term may (most probably will)
        /// be lower-cased automatically. It <b>will</b> go through the default Analyzer.
        /// <para/>
        /// Overrides super class, by passing terms through analyzer.
        /// </summary>
        /// <param name="field">Name of the field query will use.</param>
        /// <param name="termStr">Term to use for building term for the query
        /// (<b>without</b> trailing '*' character!)</param>
        /// <returns>Resulting <see cref="Query"/> built for the term</returns>
        protected internal override Query GetWildcardQuery(string field, string termStr)
        {
            if (termStr is null)
            {
                //can't imagine this would ever happen
                throw new ParseException("Passed null value as term to GetWildcardQuery");
            }
            if (!AllowLeadingWildcard && (termStr.StartsWith("*", StringComparison.Ordinal) || termStr.StartsWith("?", StringComparison.Ordinal)))
            {
                throw new ParseException("'*' or '?' not allowed as first character in WildcardQuery"
                                        + " unless AllowLeadingWildcard returns true");
            }

            Match wildcardMatcher = wildcardPattern.Match(termStr);
            StringBuilder sb = new StringBuilder();
            int last = 0;

            while (wildcardMatcher.Success)
            {
                // continue if escaped char
                if (wildcardMatcher.Groups[1].Success)
                {
                    wildcardMatcher = wildcardMatcher.NextMatch();
                    continue;
                }

                if (wildcardMatcher.Index > last)
                {
                    string chunk = termStr.Substring(last, wildcardMatcher.Index - last);
                    string analyzed = AnalyzeSingleChunk(field, termStr, chunk);
                    sb.Append(analyzed);
                }

                //append the wildcard character
                sb.Append(wildcardMatcher.Groups[2]);

                last = wildcardMatcher.Index + wildcardMatcher.Length;
                wildcardMatcher = wildcardMatcher.NextMatch();
            }
            if (last < termStr.Length)
            {
                sb.Append(AnalyzeSingleChunk(field, termStr, termStr.Substring(last)));
            }
            return base.GetWildcardQuery(field, sb.ToString());
        }

        /// <summary>
        /// Called when parser parses an input term
        /// that uses prefix notation; that is, contains a single '*' wildcard
        /// character as its last character. Since this is a special case
        /// of generic wildcard term, and such a query can be optimized easily,
        /// this usually results in a different query object.
        /// <para/>
        /// Depending on analyzer and settings, a prefix term may (most probably will)
        /// be lower-cased automatically. It <b>will</b> go through the default Analyzer.
        /// <para/>
        /// Overrides super class, by passing terms through analyzer.
        /// </summary>
        /// <param name="field">Name of the field query will use.</param>
        /// <param name="termStr">Term to use for building term for the query (<b>without</b> trailing '*' character!)</param>
        /// <returns>Resulting <see cref="Query"/> built for the term</returns>
        protected internal override Query GetPrefixQuery(string field, string termStr)
        {
            string analyzed = AnalyzeSingleChunk(field, termStr, termStr);
            return base.GetPrefixQuery(field, analyzed);
        }

        /// <summary>
        /// Called when parser parses an input term that has the fuzzy suffix (~) appended.
        /// <para/>
        /// Depending on analyzer and settings, a fuzzy term may (most probably will)
        /// be lower-cased automatically. It <b>will</b> go through the default Analyzer.
        /// <para/>
        /// Overrides super class, by passing terms through analyzer.
        /// </summary>
        /// <param name="field">Name of the field query will use.</param>
        /// <param name="termStr">Term to use for building term for the query</param>
        /// <param name="minSimilarity"></param>
        /// <returns>Resulting <see cref="Query"/> built for the term</returns>
        protected internal override Query GetFuzzyQuery(string field, string termStr, float minSimilarity)
        {
            string analyzed = AnalyzeSingleChunk(field, termStr, termStr);
            return base.GetFuzzyQuery(field, analyzed, minSimilarity);
        }

        /// <summary>
        /// Returns the analyzed form for the given chunk.
        /// 
        /// If the analyzer produces more than one output token from the given chunk,
        /// a ParseException is thrown.
        /// </summary>
        /// <param name="field">The target field</param>
        /// <param name="termStr">The full term from which the given chunk is excerpted</param>
        /// <param name="chunk">The portion of the given termStr to be analyzed</param>
        /// <returns>The result of analyzing the given chunk</returns>
        /// <exception cref="ParseException">ParseException when analysis returns other than one output token</exception>
        protected internal virtual string AnalyzeSingleChunk(string field, string termStr, string chunk)
        {
            string analyzed = null;
            TokenStream stream = null;
            try
            {
                stream = Analyzer.GetTokenStream(field, chunk);
                stream.Reset();
                ICharTermAttribute termAtt = stream.GetAttribute<ICharTermAttribute>();
                // get first and hopefully only output token
                if (stream.IncrementToken())
                {
                    analyzed = termAtt.ToString();

                    // try to increment again, there should only be one output token
                    StringBuilder multipleOutputs = null;
                    while (stream.IncrementToken())
                    {
                        if (null == multipleOutputs)
                        {
                            multipleOutputs = new StringBuilder();
                            multipleOutputs.Append('"');
                            multipleOutputs.Append(analyzed);
                            multipleOutputs.Append('"');
                        }
                        multipleOutputs.Append(',');
                        multipleOutputs.Append('"');
                        multipleOutputs.Append(termAtt.ToString());
                        multipleOutputs.Append('"');
                    }
                    stream.End();
                    if (null != multipleOutputs)
                    {
                        throw new ParseException(
                            string.Format(@"Analyzer created multiple terms for ""{0}"": {1}", chunk, multipleOutputs.ToString()));
                    }
                }
                else
                {
                    // nothing returned by analyzer.  Was it a stop word and the user accidentally
                    // used an analyzer with stop words?
                    stream.End();
                    throw new ParseException(string.Format(@"Analyzer returned nothing for ""{0}""", chunk));
                }
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw new ParseException(
                    string.Format(@"IO error while trying to analyze single term: ""{0}""", termStr), e);
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(stream);
            }
            return analyzed;
        }
    }
}
