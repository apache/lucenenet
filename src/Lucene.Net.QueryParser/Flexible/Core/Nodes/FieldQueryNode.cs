using J2N.Text;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using System.Globalization;
using System.Text;

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
    /// A <see cref="FieldQueryNode"/> represents a element that contains field/text tuple
    /// </summary>
    public class FieldQueryNode : QueryNode, IFieldValuePairQueryNode<string>, ITextableQueryNode
    {
        /// <summary>
        /// The term's field
        /// </summary>
        protected string m_field;

        /// <summary>
        /// The term's text.
        /// </summary>
        protected ICharSequence m_text;

        /// <summary>
        /// The term's begin position.
        /// </summary>
        protected int m_begin;

        /// <summary>
        /// The term's end position.
        /// </summary>
        protected int m_end;

        /// <summary>
        /// The term's position increment.
        /// </summary>
        protected int m_positionIncrement;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">field name</param>
        /// <param name="text">value</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
         // LUCENENET specific overload for passing text as string
        public FieldQueryNode(string field, string text, int begin,
            int end)
            : this(field, text.AsCharSequence(), begin, end)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">field name</param>
        /// <param name="text">value</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
         // LUCENENET specific overload for passing text as StringBuilder
        public FieldQueryNode(string field, StringBuilder text, int begin,
            int end)
            : this(field, text.AsCharSequence(), begin, end)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">field name</param>
        /// <param name="text">value</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
        public FieldQueryNode(string field, ICharSequence text, int begin,
            int end)
        {
            this.m_field = field;
            this.m_text = text;
            this.m_begin = begin;
            this.m_end = end;
            this.IsLeaf = true;

        }

        protected virtual string GetTermEscaped(IEscapeQuerySyntax escaper)
        {
            return escaper.Escape(this.m_text, CultureInfo.InvariantCulture /*Locale.getDefault()*/, EscapeQuerySyntaxType.NORMAL).ToString();
        }

        protected virtual string GetTermEscapeQuoted(IEscapeQuerySyntax escaper)
        {
            return escaper.Escape(this.m_text, CultureInfo.InvariantCulture /*Locale.getDefault()*/, EscapeQuerySyntaxType.STRING).ToString();
        }


        public override string ToQueryString(IEscapeQuerySyntax escaper)
        {
            if (IsDefaultField(this.m_field))
            {
                return GetTermEscaped(escaper);
            }
            else
            {
                return this.m_field + ":" + GetTermEscaped(escaper);
            }
        }


        public override string ToString()
        {
            return "<field start='" + this.m_begin + "' end='" + this.m_end + "' field='"
                + this.m_field + "' text='" + this.m_text + "'/>";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>the term</returns>
        public virtual string GetTextAsString()
        {
            if (this.m_text is null)
                return null;
            else
                return this.m_text.ToString();
        }

        // LUCENENET TODO: this method is not required because Field is already type string in .NET
        /// <summary>
        /// null if the field was not specified in the query string
        /// </summary>
        /// <returns>the field</returns>
        public virtual string GetFieldAsString()
        {
            if (this.m_field is null)
                return null;
            else
                return this.m_field.ToString();
        }

        public virtual int Begin
        {
            get => this.m_begin;
            set => this.m_begin = value;
        }

        public virtual int End
        {
            get => this.m_end;
            set => this.m_end = value;
        }

        public virtual string Field
        {
            get => this.m_field;
            set => this.m_field = value;
        }

        public virtual int PositionIncrement
        {
            get => this.m_positionIncrement;
            set => this.m_positionIncrement = value;
        }

        /// <summary>
        /// Gets or Sets the "original" form of the term.
        /// </summary>
        public virtual ICharSequence Text
        {
            get => this.m_text;
            set => this.m_text = value;
        }

        public override IQueryNode CloneTree()
        {
            FieldQueryNode fqn = (FieldQueryNode)base.CloneTree();
            fqn.m_begin = this.m_begin;
            fqn.m_end = this.m_end;
            fqn.m_field = this.m_field;
            fqn.m_text = this.m_text;
            fqn.m_positionIncrement = this.m_positionIncrement;
            fqn.m_toQueryStringIgnoreFields = this.m_toQueryStringIgnoreFields;

            return fqn;
        }

        public virtual string Value
        {
            get => Text.ToString();
            set => Text = value.AsCharSequence();
        }
    }
}
