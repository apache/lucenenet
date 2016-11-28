using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// A {@link FieldQueryNode} represents a element that contains field/text tuple
    /// </summary>
    public class FieldQueryNode : QueryNodeImpl, IFieldValuePairQueryNode<string>, ITextableQueryNode
    {
        /**
   * The term's field
   */
        protected string field;

        /**
         * The term's text.
         */
        protected ICharSequence text;

        /**
         * The term's begin position.
         */
        protected int begin;

        /**
         * The term's end position.
         */
        protected int end;

        /**
         * The term's position increment.
         */
        protected int positionIncrement;

        /**
         * @param field
         *          - field name
         * @param text
         *          - value
         * @param begin
         *          - position in the query string
         * @param end
         *          - position in the query string
         */
         // LUCENENET specific overload for passing text as string
        public FieldQueryNode(string field, string text, int begin,
            int end)
            : this(field, new StringCharSequenceWrapper(text), begin, end)
        {
        }

        /**
         * @param field
         *          - field name
         * @param text
         *          - value
         * @param begin
         *          - position in the query string
         * @param end
         *          - position in the query string
         */
        public FieldQueryNode(string field, ICharSequence text, int begin,
            int end)
        {
            this.field = field;
            this.text = text;
            this.begin = begin;
            this.end = end;
            this.SetLeaf(true);

        }

        protected string GetTermEscaped(IEscapeQuerySyntax escaper)
        {
            return escaper.Escape(this.text, CultureInfo.InvariantCulture /*Locale.getDefault()*/, EscapeQuerySyntax.Type.NORMAL).ToString();
        }

        protected string GetTermEscapeQuoted(IEscapeQuerySyntax escaper)
        {
            return escaper.Escape(this.text, CultureInfo.InvariantCulture /*Locale.getDefault()*/, EscapeQuerySyntax.Type.STRING).ToString();
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

        /**
         * @return the term
         */
        public string GetTextAsString()
        {
            if (this.text == null)
                return null;
            else
                return this.text.ToString();
        }

        /**
         * returns null if the field was not specified in the query string
         * 
         * @return the field
         */
        public string GetFieldAsString()
        {
            if (this.field == null)
                return null;
            else
                return this.field.ToString();
        }

        public int GetBegin()
        {
            return this.begin;
        }

        public void SetBegin(int begin)
        {
            this.begin = begin;
        }

        public int GetEnd()
        {
            return this.end;
        }

        public void setEnd(int end)
        {
            this.end = end;
        }

        public virtual string Field
        {
            get { return this.field; }
            set { this.field = value; }
        }

        //      @Override
        //public CharSequence getField()
        //      {
        //          return this.field;
        //      }

        //      @Override
        //public void setField(CharSequence field)
        //      {
        //          this.field = field;
        //      }

        public int GetPositionIncrement()
        {
            return this.positionIncrement;
        }

        public void SetPositionIncrement(int pi)
        {
            this.positionIncrement = pi;
        }

        public virtual ICharSequence Text
        {
            get { return this.text; }
            set { this.text = value; }
        }

        ///**
        // * Returns the term.
        // * 
        // * @return The "original" form of the term.
        // */

        //public override string GetText()
        //{
        //    return this.text;
        //}

        ///**
        // * @param text
        // *          the text to set
        // */

        //public override void SetText(string text)
        //{
        //    this.text = text;
        //}


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
            set { Text = new StringCharSequenceWrapper(value); }
        }

        //public override string GetValue()
        //{
        //    return GetText();
        //}


        //public override void SetValue(string value)
        //{
        //    SetText(value);
        //}
    }
}
