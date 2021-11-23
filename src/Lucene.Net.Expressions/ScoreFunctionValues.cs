using Lucene.Net.Diagnostics;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using System;
using System.Diagnostics;
using System.IO;

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
    /// A utility class to allow expressions to access the score as a
    /// <see cref="Lucene.Net.Queries.Function.FunctionValues"/>.
    /// </summary>
    internal class ScoreFunctionValues : DoubleDocValues
    {
        internal readonly Scorer scorer;

        internal ScoreFunctionValues(ValueSource parent, Scorer scorer)
            : base(parent)
        {
            this.scorer = scorer;
        }

        public override double DoubleVal(int document)
        {
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(document == scorer.DocID);

                // LUCENENET specific: The explicit cast to float is required here to prevent us from losing precision on x86 .NET Framework with optimizations enabled
                return (float)scorer.GetScore();
            }
            catch (Exception exception) when (exception.IsIOException())
            {
                throw RuntimeException.Create(exception);
            }
        }
    }
}
