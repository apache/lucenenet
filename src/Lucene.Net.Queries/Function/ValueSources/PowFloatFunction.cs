// Lucene version compatibility level 4.8.1
using System;

namespace Lucene.Net.Queries.Function.ValueSources
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
    /// Function to raise the base "a" to the power "b"
    /// <para/>
    /// NOTE: This was PowFloatFunction in Lucene
    /// </summary>
    public class PowSingleFunction : DualSingleFunction
    {
        /// <param name="a">  the base. </param>
        /// <param name="b">  the exponent. </param>
        public PowSingleFunction(ValueSource a, ValueSource b)
            : base(a, b)
        {
        }

        protected override string Name => "pow";

        protected override float Func(int doc, FunctionValues aVals, FunctionValues bVals)
        {
            return (float)Math.Pow(aVals.SingleVal(doc), bVals.SingleVal(doc));
        }
    }
}