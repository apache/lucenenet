using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Util;
using System.Globalization;

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
    /// This query node represents a field query that holds a numeric value. It is
    /// similar to <see cref="FieldQueryNode"/>, however the <see cref="Value"/> returns an
    /// <see cref="object"/> representing a .NET numeric type.
    /// </summary>
    /// <seealso cref="Standard.Config.NumericConfig"/>
    public class NumericQueryNode : QueryNode, IFieldValuePairQueryNode<J2N.Numerics.Number>
    {
        private NumberFormat numberFormat;

        private string field;

        private J2N.Numerics.Number value;

        /// <summary>
        /// Creates a <see cref="NumericQueryNode"/> object using the given field,
        /// <see cref="object"/> (representing a .NET numeric type) value and <see cref="Util.NumberFormat"/> used to convert the value to
        /// <see cref="string"/>.
        /// </summary>
        /// <param name="field">the field associated with this query node</param>
        /// <param name="value">the value hold by this node</param>
        /// <param name="numberFormat">the <see cref="Util.NumberFormat"/> used to convert the value to <see cref="string"/></param>
        public NumericQueryNode(string field, J2N.Numerics.Number value,
            NumberFormat numberFormat)
            : base()
        {
            NumberFormat = numberFormat;
            Field = field;
            Value = value;

        }

        /// <summary>
        /// Gets or Sets the field associated with this node.
        /// </summary>
        public virtual string Field
        {
            get => this.field;
            set => this.field = value;
        }

        /// <summary>
        /// This method is used to get the value converted to <see cref="string"/> and
        /// escaped using the given <see cref="IEscapeQuerySyntax"/>.
        /// </summary>
        /// <param name="escaper">The <see cref="IEscapeQuerySyntax"/> used to escape the value <see cref="string"/></param>
        /// <returns>The value converted to <see cref="string"/> and escaped</returns>
        protected string GetTermEscaped(IEscapeQuerySyntax escaper)
        {
            return escaper.Escape(numberFormat.Format(this.value),
                CultureInfo.CurrentCulture, EscapeQuerySyntaxType.NORMAL);
        }

        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            if (IsDefaultField(this.field))
            {
                return GetTermEscaped(escapeSyntaxParser);
            }
            else
            {
                return this.field + ":" + GetTermEscaped(escapeSyntaxParser);
            }
        }

        /// <summary>
        /// Gets or Sets the <see cref="Util.NumberFormat"/> used to convert the value to <see cref="string"/>.
        /// </summary>
        public virtual NumberFormat NumberFormat
        {
            get => this.numberFormat;
            set => this.numberFormat = value;
        }

        /// <summary>
        /// Gets or Sets the numeric value as a <see cref="J2N.Numerics.Number"/>.
        /// </summary>
        public virtual J2N.Numerics.Number Value
        {
            get => value;
            set => this.value = value;
        }

        public override string ToString()
        {
            return "<numeric field='" + this.field + "' number='"
                + numberFormat.Format(value) + "'/>";
        }
    }
}
