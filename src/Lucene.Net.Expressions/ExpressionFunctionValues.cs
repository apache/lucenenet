using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.DocValues;
using System;

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
    /// A <see cref="Lucene.Net.Queries.Function.FunctionValues"/> which evaluates an expression
    /// </summary>
    internal class ExpressionFunctionValues : DoubleDocValues
    {
        internal readonly Expression expression;
        internal readonly FunctionValues[] functionValues;

        internal int currentDocument = -1;
        internal double currentValue;

        internal ExpressionFunctionValues(ValueSource parent, Expression expression, FunctionValues[] functionValues) 
            : base(parent)
        {
            this.expression = expression ?? throw new ArgumentNullException(nameof(expression)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            this.functionValues = functionValues ?? throw new ArgumentNullException(nameof(functionValues)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        public override double DoubleVal(int document)
        {
            if (currentDocument != document)
            {
                currentDocument = document;
                currentValue = expression.Evaluate(document, functionValues);
            }
            return currentValue;
        }
    }
}
