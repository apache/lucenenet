/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Diagnostics;

namespace Lucene.Net.Randomized
{
    [Serializable]
    public class IllegalStateException : Exception
    {
        private StackTrace trace;
        private string traceAsString;

        public override string StackTrace
        {
            get
            {
                if (this.trace != null)
                    return this.trace.ToString();

                if (!string.IsNullOrEmpty(this.traceAsString))
                    return this.traceAsString;

                return base.StackTrace;
            }
        }

        public IllegalStateException()
        {
        }

        public IllegalStateException(string message)
            : base(message)
        {
        }

        public IllegalStateException(string message, StackTrace trace)
            : base(message)
        {
            this.trace = trace;
        }

        public IllegalStateException(string message, Exception inner)
            : base(message, inner)
        {
            this.traceAsString = inner.StackTrace;
        }

        protected IllegalStateException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}