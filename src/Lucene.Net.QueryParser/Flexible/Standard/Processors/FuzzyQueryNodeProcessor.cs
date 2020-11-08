using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using System;
using System.Collections.Generic;

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
    /// This processor iterates the query node tree looking for every
    /// <see cref="FuzzyQueryNode"/>, when this kind of node is found, it checks on the
    /// query configuration for
    /// <see cref="ConfigurationKeys.FUZZY_CONFIG"/>, gets the
    /// fuzzy prefix length and default similarity from it and set to the fuzzy node.
    /// For more information about fuzzy prefix length check: <see cref="Search.FuzzyQuery"/>.
    /// </summary>
    /// <seealso cref="ConfigurationKeys.FUZZY_CONFIG"/>
    /// <seealso cref="Search.FuzzyQuery"/>
    /// <seealso cref="FuzzyQueryNode"/>
    public class FuzzyQueryNodeProcessor : QueryNodeProcessor
    {
        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            if (node is FuzzyQueryNode fuzzyNode)
            {
                QueryConfigHandler config = GetQueryConfigHandler();

                FuzzyConfig fuzzyConfig; // LUCENENET: IDE0059: Remove unnecessary value assignment

                if (config != null && (fuzzyConfig = config.Get(ConfigurationKeys.FUZZY_CONFIG)) != null)
                {
                    fuzzyNode.PrefixLength = fuzzyConfig.PrefixLength;

                    if (fuzzyNode.Similarity < 0)
                    {
                        fuzzyNode.Similarity = fuzzyConfig.MinSimilarity;
                    }
                }
                else if (fuzzyNode.Similarity < 0)
                {
                    throw new ArgumentException("No FUZZY_CONFIG set in the config");
                }
            }

            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}
