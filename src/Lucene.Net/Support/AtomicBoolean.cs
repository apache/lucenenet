using System;
using System.Threading;

namespace Lucene.Net.Support
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

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class AtomicBoolean
    {
        private int value = 0;

        public AtomicBoolean()
            : this(false)
        {
        }

        public AtomicBoolean(bool initialValue)
        {
            value = initialValue ? 1 : 0;
        }

        public bool Get()
        {
            return value == 1 ? true : false;
        }

        public bool CompareAndSet(bool expect, bool update)
        {
            int e = expect ? 1 : 0;
            int u = update ? 1 : 0;

            int original = Interlocked.CompareExchange(ref value, u, e);

            return original == e;
        }

        public void Set(bool newValue)
        {
            Interlocked.Exchange(ref value, newValue ? 1 : 0);
        }

        public bool GetAndSet(bool newValue)
        {
            return Interlocked.Exchange(ref value, newValue ? 1 : 0) == 1;
        }

        public override string ToString()
        {
            return value == 1 ? bool.TrueString : bool.FalseString;
        }
    }
}