using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
#nullable enable

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
    /// LUCENENET specific class to provide some additional functionality around stack traces.
    /// </summary>
    internal static class StackTraceHelper
    {
        /// <summary>
        /// Matches the StackTrace for a method name.
        /// <para/>
        /// IMPORTANT: To make the tests pass in release mode, the method(s) named here
        /// must be decorated with <c>[MethodImpl(MethodImplOptions.NoInlining)]</c>.
        /// However, do not add this attribute unless you determine it is necessary, as it can
        /// harm performance. Always add two-way traceability to the method(s) in question
        /// by using the <c>nameof</c> operator to reference the method name in the test,
        /// and add a comment at the point of use of the <see cref="MethodImplAttribute"/>
        /// of which test(s) require it.
        /// </summary>
        public static bool DoesStackTraceContainMethod(string methodName)
        {
            if (methodName is null)
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            StackTrace trace = new StackTrace();
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            StackFrame[] frames = trace.GetFrames() ?? Array.Empty<StackFrame>(); // NOTE: .NET Framework can return null here

            foreach (var frame in frames)
            {
                if (frame.GetMethod()?.Name.Equals(methodName, StringComparison.Ordinal) == true)
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
            if (className is null)
            {
                throw new ArgumentNullException(nameof(className));
            }
            if (methodName is null)
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            StackTrace trace = new StackTrace();
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            StackFrame[] frames = trace.GetFrames() ?? Array.Empty<StackFrame>(); // NOTE: .NET Framework can return null here

            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                if (method?.DeclaringType?.Name.Equals(className, StringComparison.Ordinal) == true
                    && method.Name.Equals(methodName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
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
            if (destination is null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.WriteLine(new StackTrace(skipFrames: 1).ToString());
        }
    }
}
