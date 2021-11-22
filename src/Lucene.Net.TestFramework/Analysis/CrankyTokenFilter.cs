using Lucene.Net.Analysis;
using System;
using System.IO;

namespace Lucene.Net.TestFramework.Analysis
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
    /// Throws <see cref="IOException"/> from random <see cref="TokenStream"/> methods.
    /// <para/>
    /// This can be used to simulate a buggy analyzer in <see cref="Lucene.Net.Index.IndexWriter"/>,
    /// where we must delete the document but not abort everything in the buffer.
    /// </summary>
    public sealed class CrankyTokenFilter : TokenFilter
    {
        internal readonly Random random;
        internal int thingToDo;

        /// <summary>
        /// Creates a new <see cref="CrankyTokenFilter"/>.
        /// </summary>
        public CrankyTokenFilter(TokenStream input, Random random)
            : base(input)
        {
            this.random = random;
        }

        public override bool IncrementToken()
        {
            if (thingToDo == 0 && random.nextBoolean())
            {
                throw new IOException("Fake IOException from TokenStream.IncrementToken()");
            }
            return m_input.IncrementToken();
        }

        public override void End()
        {
            base.End();
            if (thingToDo == 1 && random.nextBoolean())
            {
                throw new IOException("Fake IOException from TokenStream.End()");
            }
        }

        public override void Reset()
        {
            base.Reset();
            thingToDo = random.Next(100);
            if (thingToDo == 2 && random.nextBoolean())
            {
                throw new IOException("Fake IOException from TokenStream.Reset()");
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (thingToDo == 3 && random.nextBoolean())
                {
                    throw new IOException("Fake IOException from TokenStream.Dispose(bool)");
                }
            }
        }
    }
}
