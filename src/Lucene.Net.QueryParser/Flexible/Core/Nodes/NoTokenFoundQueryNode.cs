using Lucene.Net.QueryParsers.Flexible.Core.Parser;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
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
    /// A <see cref="NoTokenFoundQueryNode"/> is used if a term is convert into no tokens
    /// by the tokenizer/lemmatizer/analyzer (null).
    /// </summary>
    public class NoTokenFoundQueryNode : DeletedQueryNode
    {
        public NoTokenFoundQueryNode()
        {
        }

        public override string ToQueryString(IEscapeQuerySyntax escaper)
        {
            return "[NTF]";
        }

        public override string ToString()
        {
            return "<notokenfound/>";
        }

        public override IQueryNode CloneTree()
        {
            NoTokenFoundQueryNode clone = (NoTokenFoundQueryNode)base.CloneTree();

            // nothing to do here

            return clone;
        }
    }
}
