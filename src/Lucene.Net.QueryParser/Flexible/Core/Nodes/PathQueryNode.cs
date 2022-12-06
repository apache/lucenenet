using J2N.Text;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
    /// A <see cref="PathQueryNode"/> is used to store queries like
    /// /company/USA/California /product/shoes/brown. QueryText are objects that
    /// contain the text, begin position and end position in the query.
    /// <para>
    /// Example how the text parser creates these objects:
    /// </para>
    /// <code>
    /// IList&lt;PathQueryNode.QueryText&gt; values = new List&lt;PathQueryNode.QueryText&gt;();
    /// values.Add(new PathQueryNode.QueryText("company", 1, 7));
    /// values.Add(new PathQueryNode.QueryText("USA", 9, 12));
    /// values.Add(new PathQueryNode.QueryText("California", 14, 23));
    /// QueryNode q = new PathQueryNode(values);
    /// </code>
    /// </summary>
    public class PathQueryNode : QueryNode
    {
        /// <summary>
        /// Term text with a beginning and end position
        /// </summary>
        public class QueryText // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
        {
            private string value = null;

            /// <summary>
            /// != null The term's begin position.
            /// </summary>
            private int begin;

            /// <summary>
            /// The term's end position.
            /// </summary>
            private int end;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="value">text value</param>
            /// <param name="begin">position in the query string</param>
            /// <param name="end">position in the query string</param>
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

            /// <summary>
            /// Gets the value
            /// </summary>
            public virtual string Value => value;

            /// <summary>
            /// Gets the begin
            /// </summary>
            public virtual int Begin => begin;

            /// <summary>
            /// Gets the end
            /// </summary>
            public virtual int End => end;

            public override string ToString()
            {
                return value + ", " + begin + ", " + end;
            }
        }

        private IList<QueryText> values = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathElements">List of QueryText objects</param>
        public PathQueryNode(IList<QueryText> pathElements)
        {
            this.values = pathElements;
            if (pathElements.Count <= 1)
            {
                // this should not happen
                throw RuntimeException.Create(
                    "PathQuerynode requires more 2 or more path elements.");
            }
        }

        /// <summary>
        /// Gets or Sets the a List with all QueryText elements
        /// </summary>
        /// <returns>QueryText List size</returns>
        public virtual IList<QueryText> PathElements
        {
            get => values;
            set => this.values = value;
        }

        /// <summary>
        /// Returns the a specific QueryText element
        /// </summary>
        /// <param name="index"></param>
        /// <returns>QueryText List size</returns>
        public virtual QueryText GetPathElement(int index)
        {
            return values[index];
        }

        /// <summary>
        /// Returns the <see cref="string"/> value of a specific QueryText element
        /// </summary>
        /// <returns>The <see cref="string"/> for a specific QueryText element</returns>
        public virtual string GetFirstPathElement()
        {
            return values[0].Value;
        }

        /// <summary>
        /// Returns a List QueryText element from position <paramref name="startIndex"/> 
        /// </summary>
        /// <param name="startIndex"></param>
        /// <returns>a List QueryText element from position <paramref name="startIndex"/></returns>
        public virtual IList<QueryText> GetPathElements(int startIndex)
        {
            IList<PathQueryNode.QueryText> rValues = new JCG.List<PathQueryNode.QueryText>();
            for (int i = startIndex; i < this.values.Count; i++)
            {
                rValues.Add((QueryText)this.values[i].Clone());
            }
            return rValues;
        }

        private string GetPathString()
        {
            StringBuilder path = new StringBuilder();

            foreach (QueryText pathelement in values)
            {
                path.Append('/').Append(pathelement.Value);
            }
            return path.ToString();
        }


        public override string ToQueryString(IEscapeQuerySyntax escaper)
        {
            StringBuilder path = new StringBuilder();
            path.Append('/').Append(GetFirstPathElement());

            foreach (QueryText pathelement in GetPathElements(1))
            {
                string value = escaper.Escape(new StringCharSequence(pathelement.Value), 
                    CultureInfo.InvariantCulture, EscapeQuerySyntaxType.STRING).ToString();
                path.Append("/\"").Append(value).Append("\"");
            }
            return path.ToString();
        }

        public override string ToString()
        {
            QueryText text = this.values[0];

            return "<path start='" + text.Begin + "' end='" + text.End + "' path='"
                + GetPathString() + "'/>";
        }

        public override IQueryNode CloneTree()
        {
            PathQueryNode clone = (PathQueryNode)base.CloneTree();

            // copy children
            if (this.values != null)
            {
                IList<QueryText> localValues = new JCG.List<QueryText>();
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
