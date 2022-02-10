using Lucene.Net.Search;
using System.Text;

namespace Lucene.Net.Expressions
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
    /// A <see cref="Lucene.Net.Search.SortField"/> which sorts documents by the evaluated value of an expression for each document
    /// </summary>
    internal class ExpressionSortField : SortField
    {
        private readonly ExpressionValueSource source;

        internal ExpressionSortField(string name, ExpressionValueSource source, bool reverse) 
            : base(name, SortFieldType.CUSTOM, reverse)
        {
            this.source = source;
        }

        public override FieldComparer GetComparer(int numHits, int sortPos)
        {
            return new ExpressionComparer(source, numHits);
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((source is null) ? 0 : source.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            ExpressionSortField other = (ExpressionSortField)obj;
            if (source is null)
            {
                if (other.source != null)
                {
                    return false;
                }
            }
            else
            {
                if (!source.Equals(other.source))
                {
                    return false;
                }
            }
            return true;
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("<expr \"");
            buffer.Append(Field);
            buffer.Append("\">");
            if (IsReverse)
            {
                buffer.Append('!');
            }
            return buffer.ToString();
        }

        public override bool NeedsScores => source.NeedsScores;
    }
}
