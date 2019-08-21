namespace Lucene.Net.Diagnostics
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
    /// Provides a set of methods that help debug your code.
    /// </summary>
    internal static class Debug
    {
        /// <summary>
        /// Checks for a condition; if the condition is <c>false</c>, throws an <see cref="AssertionException"/>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        public static void Assert(bool condition)
        {
            if (!condition)
                throw new AssertionException();
        }

        /// <summary>
        /// Checks for a condition; if the condition is <c>false</c>, throws an <see cref="AssertionException"/> with the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="message">The message to use </param>
        public static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new AssertionException(message);
        }
    }
}
