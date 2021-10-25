using System;
using System.Diagnostics;

namespace Lucene.Net.Util
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
    /// LUCENENET specific class to normalize stack trace behavior between different .NET Framework and .NET Standard 1.x,
    /// which did not support the StackTrace class.
    /// </summary>
    public static class StackTraceHelper
    {
        /// <summary>
        /// Matches the StackTrace for a method name.
        /// <para/>
        /// IMPORTANT: To make the tests pass in release mode, the method(s) named here 
        /// must be decorated with <c>[MethodImpl(MethodImplOptions.NoInlining)]</c>.
        /// </summary>
        public static bool DoesStackTraceContainMethod(string methodName)
        {
            StackTrace trace = new StackTrace();
            foreach (var frame in trace.GetFrames())
            {
                if (frame.GetMethod().Name.Equals(methodName, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Matches the StackTrace for a particular class (not fully-qualified) and method name.
        /// <para/>
        /// IMPORTANT: To make the tests pass in release mode, the method(s) named here 
        /// must be decorated with <c>[MethodImpl(MethodImplOptions.NoInlining)]</c>.
        /// </summary>
        public static bool DoesStackTraceContainMethod(string className, string methodName) 
        {
            StackTrace trace = new StackTrace();
            foreach (var frame in trace.GetFrames())
            {
                var method = frame.GetMethod();
                if (method.DeclaringType.Name.Equals(className, StringComparison.Ordinal) && method.Name.Equals(methodName, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
