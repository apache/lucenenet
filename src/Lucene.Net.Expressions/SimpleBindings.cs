using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search;
using System;
using System.Collections;
using System.Collections.Generic;

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
    /// Simple class that binds expression variable names to
    /// <see cref="Lucene.Net.Search.SortField"/>s or other
    /// <see cref="Expression"/>s.
    /// <para/>
    /// Example usage:
    /// <code>
    /// SimpleBindings bindings = new SimpleBindings
    /// {
    ///     // document's text relevance score
    ///     new SortField("_score", SortFieldType.SCORE),
    ///     // integer NumericDocValues field (or from FieldCache)
    ///     new SortField("popularity", SortFieldType.INT),
    ///     // another expression
    ///     { "recency", myRecencyExpression },
    /// };
    /// // create a sort field in reverse order
    /// Sort sort = new Sort(expr.GetSortField(bindings, true));
    /// </code>
    /// @lucene.experimental
    /// </summary>
    public sealed class SimpleBindings : Bindings, IEnumerable<KeyValuePair<string, object>> // LUCENENET specific - Added collection initializer to make populating easier
    {
        internal readonly IDictionary<string, object> map = new Dictionary<string, object>();

        /// <summary>
        /// Creates a new empty <see cref="Bindings"/>
        /// </summary>
        public SimpleBindings() { }

        /// <summary>Adds a <see cref="SortField"/> to the bindings.</summary>
        /// <remarks>
        /// Adds a <see cref="SortField"/> to the bindings.
        /// <para/>
        /// This can be used to reference a DocValuesField, a field from
        /// FieldCache, the document's score, etc.
        /// </remarks>
        public void Add(SortField sortField)
        {
            map[sortField.Field] = sortField;
        }

        /// <summary>Adds an <see cref="Expression"/> to the bindings.</summary>
        /// <remarks>
        /// Adds an <see cref="Expression"/> to the bindings.
        /// <para/>
        /// This can be used to reference expressions from other expressions.
        /// </remarks>
        public void Add(string name, Expression expression)
        {
            map[name] = expression;
        }

        public override ValueSource GetValueSource(string name)
        {
            // LUCENENET NOTE: Directly looking up a missing key will throw a KeyNotFoundException
            if (!map.TryGetValue(name, out object o))
            {
                throw new ArgumentException("Invalid reference '" + name + "'");
            }
            if (o is Expression expression)
            {
                return expression.GetValueSource(this);
            }
            SortField field = (SortField)o;
            switch (field.Type)
            {
                case SortFieldType.INT32:
                    {
                        return new Int32FieldSource(field.Field, (FieldCache.IInt32Parser)field.Parser);
                    }

                case SortFieldType.INT64:
                    {
                        return new Int64FieldSource(field.Field, (FieldCache.IInt64Parser)field.Parser);
                    }

                case SortFieldType.SINGLE:
                    {
                        return new SingleFieldSource(field.Field, (FieldCache.ISingleParser)field.Parser);
                    }

                case SortFieldType.DOUBLE:
                    {
                        return new DoubleFieldSource(field.Field, (FieldCache.IDoubleParser)field.Parser);
                    }

                case SortFieldType.SCORE:
                    {
                        return GetScoreValueSource();
                    }

                default:
                    {
                        throw UnsupportedOperationException.Create();
                    }
            }
        }

        /// <summary>Traverses the graph of bindings, checking there are no cycles or missing references</summary>
        /// <exception cref="ArgumentException">if the bindings is inconsistent</exception>
        public void Validate()
        {
            foreach (object o in map.Values)
            {
                if (o is Expression expr)
                {
#if FEATURE_STACKOVERFLOWEXCEPTION__ISCATCHABLE
                    try
                    {
#endif
                        expr.GetValueSource(this);
#if FEATURE_STACKOVERFLOWEXCEPTION__ISCATCHABLE
                    }
                    catch (Exception e) when (e.IsStackOverflowError())
                    {
                        throw new ArgumentException("Recursion Error: Cycle detected originating in (" + expr.SourceText + ")");
                    }
#endif
                }
            }
        }

        // LUCENENET specific - implemented IEnumerable<KeyValuePair<string, object>> to take advantage of the collection initializer
        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() => map.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => map.GetEnumerator();
    }
}
