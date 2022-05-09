using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;

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
    /// A <see cref="Lucene.Net.Queries.Function.ValueSource"/> which uses the
    /// <see cref="Lucene.Net.Search.Scorer"/> passed through
    /// the context map by <see cref="ExpressionComparer"/>.
    /// </summary>
    internal class ScoreValueSource : ValueSource
    {
        /// <summary>
        /// <paramref name="context"/> must contain a key "scorer" which is a
        /// <see cref="Lucene.Net.Search.Scorer"/>.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
             readerContext)
        {
            Scorer v = (Scorer)context["scorer"];
            if (v is null)
            {
                throw IllegalStateException.Create("Expressions referencing the score can only be used for sorting");
            }
            return new ScoreFunctionValues(this, v);
        }

        public override bool Equals(object o)
        {
            return o == this;
        }

        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this); // LUCENENET NOTE: This is equivalent to System.identityHashCode(this) in Java
        }

        public override string GetDescription()
        {
            return "score()";
        }
    }
}
