using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Support;
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
    /// A <see cref="Lucene.Net.Queries.Function.ValueSource"/> which evaluates a
    /// <see cref="Expression"/> given the context of an <see cref="Bindings"/>.
    /// </summary>
    internal sealed class ExpressionValueSource : ValueSource
    {
        internal readonly ValueSource[] variables;
        internal readonly Expression expression;
        internal readonly bool needsScores;

        internal ExpressionValueSource(Bindings bindings, Expression expression)
        {
            if (bindings is null)
            {
                throw new ArgumentNullException(nameof(bindings)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }

            this.expression = expression ?? throw new ArgumentNullException(nameof(expression)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            variables = new ValueSource[expression.Variables.Length];
            bool needsScores = false;
            for (int i = 0; i < variables.Length; i++)
            {
                ValueSource source = bindings.GetValueSource(expression.Variables[i]);
                if (source is ScoreValueSource)
                {
                    needsScores = true;
                }
                else
                {
                    if (source is ExpressionValueSource valueSource)
                    {
                        if (valueSource.NeedsScores)
                        {
                            needsScores = true;
                        }
                    }
                    else
                    {
                        if (source is null)
                        {
                            // LUCENENET specific: Changed from RuntimeException to InvalidOperationException to match .NET conventions
                            throw IllegalStateException.Create("Internal error. Variable (" + expression.Variables[i]
                                 + ") does not exist.");
                        }
                    }
                }
                variables[i] = source;
            }
            this.needsScores = needsScores;
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            IDictionary<string, FunctionValues> valuesCache = (IDictionary<string, FunctionValues>)context["valuesCache"];
            if (valuesCache is null)
            {
                valuesCache = new Dictionary<string, FunctionValues>();
                context = new Hashtable(context)
                {
                    ["valuesCache"] = valuesCache
                };
            }
            FunctionValues[] externalValues = new FunctionValues[expression.Variables.Length];
            for (int i = 0; i < variables.Length; ++i)
            {
                string externalName = expression.Variables[i];
                if (!valuesCache.TryGetValue(externalName, out FunctionValues values))
                {
                    values = variables[i].GetValues(context, readerContext);
                    if (values is null)
                    {
                        // LUCENENET specific: Changed from RuntimeException to InvalidOperationException to match .NET conventions
#pragma warning disable IDE0016 // Use 'throw' expression
                        throw IllegalStateException.Create($"Internal error. External ({externalName}) does not exist.");
#pragma warning restore IDE0016 // Use 'throw' expression
                    }
                    valuesCache[externalName] = values;
                }
                externalValues[i] = values;
            }
            return new ExpressionFunctionValues(this, expression, externalValues);
        }

        public override SortField GetSortField(bool reverse)
        {
            return new ExpressionSortField(expression.SourceText, this, reverse);
        }

        public override string GetDescription()
        {
            return "expr(" + expression.SourceText + ")";
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + ((expression is null) ? 0 : expression.GetHashCode());
            result = prime * result + (needsScores ? 1231 : 1237);
            result = prime * result + Arrays.GetHashCode(variables);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj is null)
            {
                return false;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            ExpressionValueSource other = (ExpressionValueSource)obj;
            if (expression is null)
            {
                if (other.expression != null)
                {
                    return false;
                }
            }
            else
            {
                if (!expression.Equals(other.expression))
                {
                    return false;
                }
            }
            if (needsScores != other.needsScores)
            {
                return false;
            }
            if (!Arrays.Equals(variables, other.variables))
            {
                return false;
            }
            return true;
        }

        internal bool NeedsScores => needsScores;
    }
}
