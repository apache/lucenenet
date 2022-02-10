using J2N.Text;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using System.Collections.Generic;
using System.Globalization;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
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
    /// This processor verifies if 
    /// <see cref="ConfigurationKeys.LOWERCASE_EXPANDED_TERMS"/> is defined in the
    /// <see cref="Core.Config.QueryConfigHandler"/>. If it is and the expanded terms should be
    /// lower-cased, it looks for every <see cref="WildcardQueryNode"/>,
    /// <see cref="FuzzyQueryNode"/> and children of a <see cref="IRangeQueryNode"/> and lower-case its
    /// term.
    /// </summary>
    /// <seealso cref="ConfigurationKeys.LOWERCASE_EXPANDED_TERMS"/>.
    public class LowercaseExpandedTermsQueryNodeProcessor : QueryNodeProcessor
    {
        public LowercaseExpandedTermsQueryNodeProcessor()
        {
        }

        public override IQueryNode Process(IQueryNode queryTree)
        {
            if (GetQueryConfigHandler().TryGetValue(ConfigurationKeys.LOWERCASE_EXPANDED_TERMS, out bool lowercaseExpandedTerms)
                && lowercaseExpandedTerms)
            {
                return base.Process(queryTree);
            }

            return queryTree;
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            CultureInfo locale = GetQueryConfigHandler().Get(ConfigurationKeys.LOCALE);
            if (locale is null)
            {
                locale = CultureInfo.CurrentCulture; //Locale.getDefault();
            }

            if (node is WildcardQueryNode
                || node is FuzzyQueryNode
                || (node is FieldQueryNode && node.Parent is IRangeQueryNode)
                || node is RegexpQueryNode)
            {
                ITextableQueryNode txtNode = (ITextableQueryNode)node;
                ICharSequence text = txtNode.Text;
                txtNode.Text = text != null ? UnescapedCharSequence.ToLower(text, locale) : null;
            }

            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}
