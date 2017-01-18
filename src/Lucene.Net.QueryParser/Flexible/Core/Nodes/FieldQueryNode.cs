using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System.Globalization;

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
    public class FieldQueryNode : QueryNodeImpl, IFieldValuePairQueryNode<string>, ITextableQueryNode
    {
        /// <summary>
        /// The term's field
        /// </summary>
        protected string field;

        /// <summary>
        /// The term's text.
        /// </summary>
        protected ICharSequence text;

        /// <summary>
        /// The term's begin position.
        /// </summary>
        protected int begin;

        /// <summary>
        /// The term's end position.
        /// </summary>
        protected int end;

        /// <summary>
        /// The term's position increment.
        /// </summary>
        protected int positionIncrement;

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
            : this(field, new StringCharSequenceWrapper(text), begin, end)
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
            this.field = field;
            this.text = text;
            this.begin = begin;
            this.end = end;
            this.IsLeaf = true;

        }

        protected virtual string GetTermEscaped(IEscapeQuerySyntax escaper)
        {
            return escaper.Escape(this.text, CultureInfo.InvariantCulture /*Locale.getDefault()*/, EscapeQuerySyntaxType.NORMAL).ToString();
        }

        protected virtual string GetTermEscapeQuoted(IEscapeQuerySyntax escaper)
        {
            return escaper.Escape(this.text, CultureInfo.InvariantCulture /*Locale.getDefault()*/, EscapeQuerySyntaxType.STRING).ToString();
        }


        public override string ToQueryString(IEscapeQuerySyntax escaper)
        {
            if (IsDefaultField(this.field))
            {
                return GetTermEscaped(escaper);
            }
            else
            {
                return this.field + ":" + GetTermEscaped(escaper);
            }
        }


        public override string ToString()
        {
            return "<field start='" + this.begin + "' end='" + this.end + "' field='"
                + this.field + "' text='" + this.text + "'/>";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>the term</returns>
        public virtual string GetTextAsString()
        {
            if (this.text == null)
                return null;
            else
                return this.text.ToString();
        }

        // LUCENENET TODO: this method is not required because Field is already type string in .NET
        /// <summary>
        /// null if the field was not specified in the query string
        /// </summary>
        /// <returns>the field</returns>
        public virtual string GetFieldAsString()
        {
            if (this.field == null)
                return null;
            else
                return this.field.ToString();
        }

        public virtual int Begin
        {
            get { return this.begin; }
            set { this.begin = value; }
        }

        public virtual int End
        {
            get { return this.end; }
            set { this.end = value; }
        }

        public virtual string Field
        {
            get { return this.field; }
            set { this.field = value; }
        }

        public virtual int PositionIncrement
        {
            get { return this.positionIncrement; }
            set { this.positionIncrement = value; }
        }

        /// <summary>
        /// Gets or Sets the "original" form of the term.
        /// </summary>
        public virtual ICharSequence Text
        {
            get { return this.text; }
            set { this.text = value; }
        }

        public override IQueryNode CloneTree()
        {
            FieldQueryNode fqn = (FieldQueryNode)base.CloneTree();
            fqn.begin = this.begin;
            fqn.end = this.end;
            fqn.field = this.field;
            fqn.text = this.text;
            fqn.positionIncrement = this.positionIncrement;
            fqn.toQueryStringIgnoreFields = this.toQueryStringIgnoreFields;

            return fqn;
        }

        public virtual string Value
        {
            get { return Text.ToString(); }
            set { Text = value.ToCharSequence(); }
        }
    }
}
