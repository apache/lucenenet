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
    public class PathQueryNode : QueryNode
    {
        public class QueryText : ICloneable
        {
            string value = null;
            int begin;
            int end;

            public QueryText(string value, int begin, int end)
            {
                this.value = value;
                this.begin = begin;
                this.end = end;
            }

            public string Value
            {
                get { return value; }
            }

            public int Begin
            {
                get { return begin; }
            }

            public int End
            {
                get { return end; }
            }

            public override string ToString()
            {
                return value + ", " + begin + ", " + end;
            }

            public object Clone()
            {
                return this.MemberwiseClone();
            }
        }

        private IList<QueryText> values = null;

        public PathQueryNode(IList<QueryText> pathElements)
        {
            this.values = pathElements;
            if (pathElements.Count <= 1)
            {
                // this should not happen
                throw new ArgumentException("PathQuerynode requires more 2 or more path elements.");
            }
        }

        public IList<QueryText> PathElements
        {
            get { return values; }
            set { this.values = value; }
        }

        public QueryText GetPathElement(int index)
        {
            return values[index];
        }

        public string FirstPathElement
        {
            get { return values[0].Value; }
        }

        public IList<QueryText> GetPathElements(int startIndex)
        {
            IList<PathQueryNode.QueryText> rValues = new List<PathQueryNode.QueryText>();
            for (int i = startIndex; i < this.values.Count; i++)
            {
                try
                {
                    rValues.Add((QueryText)this.values[i].Clone());
                }
                catch (NotSupportedException)
                {
                    // this will not happen
                }
            }
            return rValues;
        }

        private string PathString
        {
            get
            {
                StringBuilder path = new StringBuilder();

                foreach (QueryText pathelement in values)
                {
                    path.Append("/").Append(pathelement.Value);
                }
                return path.ToString();
            }
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escaper)
        {
            StringBuilder path = new StringBuilder();
            path.Append("/").Append(FirstPathElement);

            foreach (QueryText pathelement in GetPathElements(1))
            {
                ICharSequence value = escaper.Escape(new StringCharSequenceWrapper(pathelement.Value), CultureInfo.CurrentCulture, EscapeQuerySyntax.Type.STRING);
                path.Append("/\"").Append(value).Append("\"");
            }
            return new StringCharSequenceWrapper(path.ToString());
        }
    }
}
