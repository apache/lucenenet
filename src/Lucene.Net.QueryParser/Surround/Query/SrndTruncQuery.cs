using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.QueryParsers.Surround.Query
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
    /// Query that matches wildcards
    /// </summary>
    public class SrndTruncQuery : SimpleTerm
    {
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("CodeQuality", "S1699:Constructors should only call non-overridable methods", Justification = "Required for continuity with Lucene's design")]
        public SrndTruncQuery(string truncated, char unlimited, char mask)
            : this(truncated, unlimited, mask, quoted: false) /* not quoted */
        {
            TruncatedToPrefixAndPattern();
        }

        // LUCENENET specific - this is for provided for subclasses to use and avoid
        // the virtual call to TruncatedToPrefixAndPattern(), which they can do
        // in their own constructor.
        protected SrndTruncQuery(string truncated, char unlimited, char mask, bool quoted)
            : base(quoted)
        {
            this.truncated = truncated;
            this.unlimited = unlimited;
            this.mask = mask;
        }

        private readonly string truncated;
        private readonly char unlimited;
        private readonly char mask;

        private string prefix;
        private BytesRef prefixRef;
        private Regex pattern;

        public virtual string Truncated => truncated;

        public override string ToStringUnquoted()
        {
            return Truncated;
        }

        protected virtual bool MatchingChar(char c)
        {
            return (c != unlimited) && (c != mask);
        }

        protected virtual void AppendRegExpForChar(char c, StringBuilder re)
        {
            if (c == unlimited)
                re.Append(".*");
            else if (c == mask)
                re.Append('.');
            else
                re.Append(c);
        }

        protected virtual void TruncatedToPrefixAndPattern()
        {
            int i = 0;
            while ((i < truncated.Length) && MatchingChar(truncated[i]))
            {
                i++;
            }
            prefix = truncated.Substring(0, i);
            prefixRef = new BytesRef(prefix);

            StringBuilder re = new StringBuilder();
            // LUCENENET NOTE: To mimic Java's matches() method, we alter
            // the Regex to match the entire string. This makes the Regex
            // fail fast when not at the beginning of the string, which is
            // more efficient than testing the length after a successful match.
            // http://stackoverflow.com/a/12547528/181087
            re.Append(@"\A(?:");
            while (i < truncated.Length)
            {
                AppendRegExpForChar(truncated[i], re);
                i++;
            }
            re.Append(@")\z");
            pattern = new Regex(re.ToString(), RegexOptions.Compiled);
        }

        public override void VisitMatchingTerms(IndexReader reader, string fieldName, SimpleTerm.IMatchingTermVisitor mtv)
        {
            int prefixLength = prefix.Length;
            Terms terms = MultiFields.GetTerms(reader, fieldName);
            if (terms != null)
            {
                TermsEnum termsEnum = terms.GetEnumerator();

                TermsEnum.SeekStatus status = termsEnum.SeekCeil(prefixRef);
                BytesRef text;
                if (status == TermsEnum.SeekStatus.FOUND)
                {
                    text = prefixRef;
                }
                else if (status == TermsEnum.SeekStatus.NOT_FOUND)
                {
                    text = termsEnum.Term;
                }
                else
                {
                    text = null;
                }

                while (true)
                {
                    if (text != null && StringHelper.StartsWith(text, prefixRef))
                    {
                        string textString = text.Utf8ToString();
                        Match matcher = pattern.Match(textString.Substring(prefixLength));
                        if (matcher.Success)
                        {
                            mtv.VisitMatchingTerm(new Term(fieldName, textString));
                        }
                    }
                    else
                    {
                        break;
                    }
                    if (termsEnum.MoveNext())
                        text = termsEnum.Term;
                    else
                        break;
                }
            }
        }
    }
}
