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
    /// A {@link PathQueryNode} is used to store queries like
    /// /company/USA/California /product/shoes/brown. QueryText are objects that
    /// contain the text, begin position and end position in the query.
    /// <para>
    /// Example how the text parser creates these objects:
    /// </para>
    /// <code>
    /// List values = new List();
    /// values.add(new PathQueryNode.QueryText("company", 1, 7));
    /// values.add(new PathQueryNode.QueryText("USA", 9, 12));
    /// values.add(new PathQueryNode.QueryText("California", 14, 23));
    /// QueryNode q = new PathQueryNode(values);
    /// </code>
    /// 
    /// </summary>
    public class PathQueryNode : QueryNodeImpl
    {
        /**
   * Term text with a beginning and end position
   */
        public class QueryText : ICloneable
        {
            internal string value = null;
            /**
             * != null The term's begin position.
             */
            internal int begin;

            /**
             * The term's end position.
             */
            internal int end;

            /**
             * @param value
             *          - text value
             * @param begin
             *          - position in the query string
             * @param end
             *          - position in the query string
             */
            public QueryText(string value, int begin, int end)
                : base()
            {
                this.value = value;
                this.begin = begin;
                this.end = end;
            }


            public virtual /*QueryText*/ object Clone()
            {
                QueryText clone = (QueryText)this.MemberwiseClone();
                clone.value = this.value;
                clone.begin = this.begin;
                clone.end = this.end;
                return clone;
            }

            /**
             * @return the value
             */
            public string GetValue()
            {
                return value;
            }

            /**
             * @return the begin
             */
            public int GetBegin()
            {
                return begin;
            }

            /**
             * @return the end
             */
            public int GetEnd()
            {
                return end;
            }

            public override string ToString()
            {
                return value + ", " + begin + ", " + end;
            }
        }

        private IList<QueryText> values = null;

        /**
         * @param pathElements
         *          - List of QueryText objects
         */
        public PathQueryNode(IList<QueryText> pathElements)
        {
            this.values = pathElements;
            if (pathElements.Count <= 1)
            {
                // this should not happen
                throw new Exception(
                    "PathQuerynode requires more 2 or more path elements.");
            }
        }

        /**
         * Returns the a List with all QueryText elements
         * 
         * @return QueryText List size
         */
        public IList<QueryText> GetPathElements()
        {
            return values;
        }

        /**
         * Returns the a List with all QueryText elements
         */
        public void SetPathElements(IList<QueryText> elements)
        {
            this.values = elements;
        }

        /**
         * Returns the a specific QueryText element
         * 
         * @return QueryText List size
         */
        public QueryText GetPathElement(int index)
        {
            return values[index];
        }

        /**
         * Returns the CharSequence value of a specific QueryText element
         * 
         * @return the CharSequence for a specific QueryText element
         */
        public string GetFirstPathElement()
        {
            return values[0].value;
        }

        /**
         * Returns a List QueryText element from position startIndex
         * 
         * @return a List QueryText element from position startIndex
         */
        public IList<QueryText> GetPathElements(int startIndex)
        {
            List<PathQueryNode.QueryText> rValues = new List<PathQueryNode.QueryText>();
            for (int i = startIndex; i < this.values.Count; i++)
            {
                //try
                //{
                rValues.Add((QueryText)this.values[i].Clone());
                //}
                //catch (CloneNotSupportedException e)
                //{
                //    // this will not happen
                //}
            }
            return rValues;
        }

        private string GetPathString()
        {
            StringBuilder path = new StringBuilder();

            foreach (QueryText pathelement in values)
            {
                path.Append("/").Append(pathelement.value);
            }
            return path.ToString();
        }


        public override string ToQueryString(IEscapeQuerySyntax escaper)
        {
            StringBuilder path = new StringBuilder();
            path.Append("/").Append(GetFirstPathElement());

            foreach (QueryText pathelement in GetPathElements(1))
            {
                string value = escaper.Escape(new StringCharSequenceWrapper(pathelement.value), 
                    CultureInfo.InvariantCulture, EscapeQuerySyntax.Type.STRING).ToString();
                path.Append("/\"").Append(value).Append("\"");
            }
            return path.ToString();
        }


        public override string ToString()
        {
            QueryText text = this.values[0];

            return "<path start='" + text.begin + "' end='" + text.end + "' path='"
                + GetPathString() + "'/>";
        }


        public override IQueryNode CloneTree()
        {
            PathQueryNode clone = (PathQueryNode)base.CloneTree();

            // copy children
            if (this.values != null)
            {
                List<QueryText> localValues = new List<QueryText>();
                foreach (QueryText value in this.values)
                {
                    localValues.Add((QueryText)value.Clone());
                }
                clone.values = localValues;
            }

            return clone;
        }
    }
}
