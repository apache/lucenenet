using System;
using System.Runtime.CompilerServices;

namespace Lucene.Net
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
    /// Extension methods to close gaps when catching exceptions in .NET.
    /// <para/>
    /// These methods make it possible to catch only the types for a general exception
    /// type in Java even though the exception inheritance structure is different in .NET
    /// and does not map 1-to-1 with Java exceptions.
    /// <para/>
    /// This class contains "overrides" for the logic in production when we want different
    /// behavior in tests. The syntax of the extension method in the test is exactly the same,
    /// but the method may behave more accurately so we can make the tests more explicit.
    /// </summary>
    internal static class ExceptionExtensions
    {
        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an IllegalArgumentException
        /// in Java.
        /// <para/>
        /// NOTE: This method differs from <see cref="Lucene.ExceptionExtensions.IsIllegalArgumentException(Exception)"/> but
        /// since they use the same name, this is the default in the tests. This tests specifically for the <see cref="ArgumentException"/>
        /// and will return <c>false</c> for <see cref="ArgumentNullException"/> or <see cref="ArgumentOutOfRangeException"/>.
        /// <para/>
        /// In a nutshell, it is better in production code to catch <see cref="ArgumentException"/> and all subclasses
        /// because that guarantees it will "just work" when we upgrade from using the Java-like <see cref="IllegalArgumentException"/>
        /// to the more specific <see cref="ArgumentNullException"/> or <see cref="ArgumentOutOfRangeException"/>. But tests generally
        /// are checking to make sure the guard clauses are implemented properly, so it is better to fail in cases where we are not precisely
        /// catching <see cref="IllegalArgumentException"/>. This way, the test can be updated and commented to indicate that we changed the behavior in .NET.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an IllegalArgumentException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIllegalArgumentException(this Exception e)
        {
            // If our exception implements IError and subclasses ArgumentException, we will ignore it.
            if (e is null || e.IsError() || e.IsAlwaysIgnored()) return false;

            return e is ArgumentException &&
                !(e is ArgumentNullException) &&     // Corresponds to NullPointerException, so we don't catch it here.
                !(e is ArgumentOutOfRangeException); // Corresponds to IndexOutOfBoundsException (and subclasses), so we don't catch it here.
        }
    }
}
