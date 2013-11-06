/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Sandbox.Queries.Regex
{
	/// <summary>
	/// Regular expression based query.
	/// </summary>
	/// <remarks>http://www.java2s.com/Open-Source/Java-Document/Net/lucene-connector/org/apache/lucene/search/regex/RegexQuery.java.htm</remarks>
	public class RegexQuery : MultiTermQuery, IRegexQueryCapable
	{
		private IRegexCapabilities regexImpl = new CSharpRegexCapabilities();
        private Term term;

		public RegexQuery(Term term)
            : base(term.Field)
		{
            this.term = term;
		}

        public Term Term
        {
            get { return this.term; }
        }

        public IRegexCapabilities RegexImplementation
        {
            set { regexImpl = value; }
            get { return regexImpl; }
        }

        protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            return new RegexTermsEnum(terms.Iterator(null), term, regexImpl);
        }
        
	    public override String ToString(String field)
        {
            StringBuilder buffer = new StringBuilder();
            if (!term.Field.Equals(field))
            {
                buffer.Append(term.Field);
                buffer.Append(":");
            }
            buffer.Append(term.Text);
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }
        
        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((regexImpl == null) ? 0 : regexImpl.GetHashCode());
            result = prime * result + ((term == null) ? 0 : term.GetHashCode());
            return result;
        }

		public override bool Equals(object obj)
		{
            if (this == obj)
            {
                return true;
            }

            if (!base.Equals(obj))
            {
                return false;
            }

            if (GetType() != obj.GetType())
            {
                return false;
            }

            RegexQuery other = (RegexQuery)obj;
            if (regexImpl == null)
            {
                if (other.regexImpl != null)
                {
                    return false;
                }
            }
            else if (!regexImpl.Equals(other.regexImpl))
            {
                return false;
            }

            if (term == null)
            {
                if (other.term != null)
                {
                    return false;
                }
            }
            else if (!term.Equals(other.term))
            {
                return false;
            }

            return true;
		}

	}
}
