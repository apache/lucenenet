using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// A <see cref="ModifierQueryNode"/> indicates the modifier value (+,-,?,NONE) for
    /// each term on the query string. For example "+t1 -t2 t3" will have a tree of:
    /// <blockquote>
    /// &lt;BooleanQueryNode&gt; &lt;ModifierQueryNode modifier="MOD_REQ"&gt; &lt;t1/&gt;
    /// &lt;/ModifierQueryNode&gt; &lt;ModifierQueryNode modifier="MOD_NOT"&gt; &lt;t2/&gt;
    /// &lt;/ModifierQueryNode&gt; &lt;t3/&gt; &lt;/BooleanQueryNode&gt;
    /// </blockquote>
    /// </summary>
    public class ModifierQueryNode : QueryNode
    {
        // LUCENENET NOTE: Modifier enum moved outside of this class

        private Modifier modifier = Nodes.Modifier.MOD_NONE;

        /// <summary>
        /// Used to store the modifier value on the original query string
        /// </summary>
        /// <param name="query">QueryNode subtree</param>
        /// <param name="mod">Modifier Value</param>
        public ModifierQueryNode(IQueryNode query, Modifier mod)
        {
            // LUCENENET: Factored out NLS/Message/IMessage so end users can optionally utilize the built-in .NET localization.
            // LUCENENET: Added paramName parameter and changed to the same error message as the default of ArgumentNullException.
            // However, we still need this to be an error type so it is not caught in StandardSyntaxParser.
            if (query is null)
                throw new QueryNodeError(QueryParserMessages.ARGUMENT_CANNOT_BE_NULL, nameof(query));

            Allocate();
            IsLeaf = false;
            Add(query);
            this.modifier = mod;
        }

        public virtual IQueryNode GetChild()
        {
            return GetChildren()[0];
        }

        public virtual Modifier Modifier => this.modifier;

        public override string ToString()
        {
            return "<modifier operation='" + this.modifier.ToString() + "'>" + "\n"
                + GetChild().ToString() + "\n</modifier>";
        }

        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            if (GetChild() is null)
                return "";

            string leftParenthensis = "";
            string rightParenthensis = "";

            if (GetChild() != null && GetChild() is ModifierQueryNode)
            {
                leftParenthensis = "(";
                rightParenthensis = ")";
            }

            if (GetChild() is BooleanQueryNode)
            {
                return this.modifier.ToLargeString() + leftParenthensis
                    + GetChild().ToQueryString(escapeSyntaxParser) + rightParenthensis;
            }
            else
            {
                return this.modifier.ToDigitString() + leftParenthensis
                    + GetChild().ToQueryString(escapeSyntaxParser) + rightParenthensis;
            }
        }

        public override IQueryNode CloneTree()
        {
            ModifierQueryNode clone = (ModifierQueryNode)base.CloneTree();

            clone.modifier = this.modifier;

            return clone;
        }

        public virtual void SetChild(IQueryNode child)
        {
            IList<IQueryNode> list = new JCG.List<IQueryNode>
            {
                child
            };
            this.Set(list);
        }
    }

    /// <summary>
    /// Modifier type: such as required (REQ), prohibited (NOT)
    /// </summary>
    public enum Modifier
    {
        MOD_NONE,
        MOD_NOT,
        MOD_REQ
    }

    public static class ModifierExtensions
    {
        // LUCENENET TODO: Work out how to override ToString() (or test this) so this string can be made
        //public static string ToString()
        //{
        //    switch (this)
        //    {
        //        case MOD_NONE:
        //            return "MOD_NONE";
        //        case MOD_NOT:
        //            return "MOD_NOT";
        //        case MOD_REQ:
        //            return "MOD_REQ";
        //    }
        //    // this code is never executed
        //    return "MOD_DEFAULT";
        //}

        public static string ToDigitString(this Modifier modifier)
        {
            switch (modifier)
            {
                case Modifier.MOD_NONE:
                    return "";
                case Modifier.MOD_NOT:
                    return "-";
                case Modifier.MOD_REQ:
                    return "+";
            }
            // this code is never executed
            return "";
        }

        public static string ToLargeString(this Modifier modifier)
        {
            switch (modifier)
            {
                case Modifier.MOD_NONE:
                    return "";
                case Modifier.MOD_NOT:
                    return "NOT ";
                case Modifier.MOD_REQ:
                    return "+";
            }
            // this code is never executed
            return "";
        }
    }
}
