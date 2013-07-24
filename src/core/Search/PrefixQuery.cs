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
using Lucene.Net.Util;

namespace Lucene.Net.Search
{
	
	/// <summary>A Query that matches documents containing terms with a specified prefix. A PrefixQuery
	/// is built by QueryParser for input like <c>app*</c>.
	/// 
	/// <p/>This query uses the 
    /// <see cref="MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT"/>
	/// rewrite method. 
	/// </summary>
	[Serializable]
	public class PrefixQuery : MultiTermQuery
	{
		private Term prefix;
		
		/// <summary>Constructs a query for terms starting with <c>prefix</c>. </summary>
		public PrefixQuery(Term prefix)
            : base(prefix.Field)
		{
			this.prefix = prefix;
		}

	    /// <summary>Returns the prefix of this query. </summary>
	    public virtual Term Prefix
	    {
	        get { return prefix; }
	    }

        protected internal override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            var tenum = terms.Iterator(null);

            if (prefix.Bytes.length == 0)
            {
                return tenum;
            }
            return new PrefixTermsEnum(tenum, prefix.Bytes);
        }

		/// <summary>Prints a user-readable version of this query. </summary>
		public override string ToString(string field)
		{
			var buffer = new StringBuilder();
			if (!prefix.Field.Equals(field))
			{
				buffer.Append(prefix.Field);
				buffer.Append(":");
			}
			buffer.Append(prefix.Text);
			buffer.Append('*');
			buffer.Append(ToStringUtils.Boost(Boost));
			return buffer.ToString();
		}
		
		public override int GetHashCode()
		{
			int prime = 31;
			int result = base.GetHashCode();
			result = prime * result + ((prefix == null) ? 0 : prefix.GetHashCode());
			return result;
		}
		
		public override bool Equals(object obj)
		{
			if (this == obj)
				return true;
			if (!base.Equals(obj))
				return false;
			if (GetType() != obj.GetType())
				return false;
			var other = (PrefixQuery) obj;
			if (prefix == null)
			{
				if (other.prefix != null)
					return false;
			}
			else if (!prefix.Equals(other.prefix))
				return false;
			return true;
		}
	}
}