using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using System;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
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
    /// This class should be extended by nodes intending to represent range queries.
    /// </summary>
    /// <typeparam name="T">the type of the range query bounds (lower and upper)</typeparam>
    public class AbstractRangeQueryNode<T> : QueryNode, IAbstractRangeQueryNode where T : IFieldableNode /*IFieldValuePairQueryNode<?>*/
    { /*IRangeQueryNode<IFieldValuePairQueryNode<?>>*/

        private bool lowerInclusive, upperInclusive;

        /// <summary>
        /// Constructs an <see cref="AbstractRangeQueryNode{T}"/>, it should be invoked only by
        /// its extenders.
        /// </summary>
        protected AbstractRangeQueryNode()
        {
            IsLeaf = false;
            Allocate();
        }

        /// <summary>
        /// Gets or Sets the field associated with this node.
        /// </summary>
        /// <seealso cref="IFieldableNode"/>
        public virtual string Field
        {
            get
            {
                string field = null;
                T lower = (T)LowerBound;
                T upper = (T)UpperBound;

                if (lower != null)
                {
                    field = lower.Field;

                }
                else if (upper != null)
                {
                    field = upper.Field;
                }

                return field;
            }
            set
            {
                T lower = (T)LowerBound;
                T upper = (T)UpperBound;

                if (lower != null)
                {
                    lower.Field = value;
                }

                if (upper != null)
                {
                    upper.Field = value;
                }
            }
        }

        /// <summary>
        /// Gets the lower bound node.
        /// </summary>
        public virtual IFieldableNode LowerBound => (IFieldableNode)GetChildren()[0];

        /// <summary>
        /// Gets the upper bound node.
        /// </summary>
        public virtual IFieldableNode UpperBound => (IFieldableNode)GetChildren()[1];

        /// <summary>
        /// Gets whether the lower bound is inclusive or exclusive. 
        /// </summary>
        /// <remarks>
        /// <c>true</c> if the lower bound is inclusive, otherwise, <c>false</c>
        /// </remarks>
        public virtual bool IsLowerInclusive => lowerInclusive;

        /// <summary>
        /// Gets whether the upper bound is inclusive or exclusive.
        /// </summary>
        /// <remarks>
        /// <c>true</c> if the upper bound is inclusive, otherwise, <c>false</c>
        /// </remarks>
        public virtual bool IsUpperInclusive => upperInclusive;

        /// <summary>
        /// Sets the lower and upper bounds.
        /// </summary>
        /// <param name="lower">the lower bound, <c>null</c> if lower bound is open</param>
        /// <param name="upper">the upper bound, <c>null</c> if upper bound is open</param>
        /// <param name="lowerInclusive"><c>true</c> if the lower bound is inclusive, otherwise, <c>false</c></param>
        /// <param name="upperInclusive"><c>true</c> if the upper bound is inclusive, otherwise, <c>false</c></param>
        /// <seealso cref="LowerBound"/>
        /// <seealso cref="UpperBound"/>
        /// <seealso cref="IsLowerInclusive"/>
        /// <seealso cref="IsUpperInclusive"/>
        public virtual void SetBounds(T lower, T upper, bool lowerInclusive,
            bool upperInclusive)
        {
            if (lower != null && upper != null)
            {
                string lowerField = StringUtils.ToString(lower.Field);
                string upperField = StringUtils.ToString(upper.Field);

                if ((upperField != null || lowerField != null)
                    && ((upperField != null && !upperField.Equals(lowerField, StringComparison.Ordinal)) || !lowerField
                        .Equals(upperField, StringComparison.Ordinal)))
                {
                    throw new ArgumentException(
                        "lower and upper bounds should have the same field name!");
                }

                this.lowerInclusive = lowerInclusive;
                this.upperInclusive = upperInclusive;

                IList<IQueryNode> children = new JCG.List<IQueryNode>(2)
                {
                    lower,
                    upper
                };

                Set(children);
            }
        }

        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            StringBuilder sb = new StringBuilder();

            T lower = (T)LowerBound;
            T upper = (T)UpperBound;

            if (lowerInclusive)
            {
                sb.Append('[');
            }
            else
            {
                sb.Append('{');
            }

            if (lower != null)
            {
                sb.Append(lower.ToQueryString(escapeSyntaxParser));
            }
            else
            {
                sb.Append("...");
            }

            sb.Append(' ');

            if (upper != null)
            {
                sb.Append(upper.ToQueryString(escapeSyntaxParser));
            }
            else
            {
                sb.Append("...");
            }

            if (upperInclusive)
            {
                sb.Append(']');
            }
            else
            {
                sb.Append('}');
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("<").Append(GetType().AssemblyQualifiedName);
            sb.Append(" lowerInclusive=").Append(IsLowerInclusive);
            sb.Append(" upperInclusive=").Append(IsUpperInclusive);
            sb.Append(">\n\t");
            sb.Append(UpperBound).Append("\n\t");
            sb.Append(LowerBound).Append("\n");
            sb.Append("</").Append(GetType().AssemblyQualifiedName).Append(">\n");

            return sb.ToString();
        }
    }

    /// <summary>
    /// LUCENENET specific interface used to identify
    /// an AbstractRangeQueryNode without referring to 
    /// its generic closing type
    /// </summary>
    public interface IAbstractRangeQueryNode : IRangeQueryNode<IFieldableNode>
    { }
}
