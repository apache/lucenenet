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
using System;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
    /// <code>MinFloatFunction</code> returns the min of it's components.
    /// </summary>
    public class MinFloatFunction : MultiFloatFunction
    {
        public MinFloatFunction(ValueSource[] sources)
            : base(sources)
        {
        }

        protected override string Name
        {
            get { return "min"; }
        }

        protected override float Func(int doc, FunctionValues[] valsArr)
        {
            if (valsArr.Length == 0)
            {
                return 0.0f;
            }
            float val = float.PositiveInfinity;
            foreach (FunctionValues vals in valsArr)
            {
                val = Math.Min(vals.FloatVal(doc), val);
            }
            return val;
        }
    }
}