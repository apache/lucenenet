using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

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

    /// <summary>
    /// LUCENENET specific class to normalize stack trace behavior between different .NET Framework and .NET Standard 1.x,
    /// which did not support the StackTrace class, and provide some additional functionality.
    /// </summary>
    internal static class StackTraceHelper
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

        /// <summary>
        /// Prints the current stack trace to the console's standard error output stream.
        /// <para />
        /// This is equivalent to Java's <c>new Throwable().printStackTrace()</c>
        /// or <c>new Exception().printStackTrace()</c>.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)] // Top frame is skipped, so we don't want to inline
        public static void PrintCurrentStackTrace()
        {
            Console.Error.WriteLine(new StackTrace(skipFrames: 1).ToString());
        }

        /// <summary>
        /// Prints the current stack trace to the specified <paramref name="destination"/>.
        /// <para />
        /// This is equivalent to Java's <c>new Throwable().printStackTrace(destination)</c>
        /// or <c>new Exception().printStackTrace(destination)</c>.
        /// </summary>
        /// <param name="destination">The destination to write the stack trace to.</param>
        [MethodImpl(MethodImplOptions.NoInlining)] // Top frame is skipped, so we don't want to inline
        public static void PrintCurrentStackTrace(TextWriter destination)
        {
            destination.WriteLine(new StackTrace(skipFrames: 1).ToString());
        }
    }
}
