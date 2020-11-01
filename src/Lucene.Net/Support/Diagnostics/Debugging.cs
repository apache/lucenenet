using J2N.Text;
using Lucene.Net.Util;
using System.Runtime.CompilerServices;

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
    internal static class Debugging
    {
        /// <summary>
        /// Allows toggling "assertions" on/off even in release builds. The default is <c>false</c>.
        /// <para/>
        /// This allows loggers and testing frameworks to enable test point messages ("TP")
        /// from <see cref="Index.IndexWriter"/>, <see cref="Index.DocumentsWriterPerThread"/>,
        /// <see cref="Index.FreqProxTermsWriterPerField"/>, <see cref="Index.StoredFieldsProcessor"/>,
        /// <see cref="Index.TermVectorsConsumer"/>, and <see cref="Index.TermVectorsConsumerPerField"/>.
        /// </summary>
        public static bool AssertsEnabled = SystemProperties.GetPropertyAsBoolean("assert", false);

        /// <summary>
        /// Checks for a condition; if the condition is <c>false</c>, throws an <see cref="AssertionException"/>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool condition)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException();
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageToFormat"/>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="messageToFormat">A string format (i.e. with {0} that will be filled with the parameters</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert<T0>(bool condition, string messageToFormat, T0 p0)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(string.Format(StringFormatter.InvariantCulture, messageToFormat, p0));
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageToFormat"/>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="messageToFormat">A string format (i.e. with {0} that will be filled with the parameters</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert<T0, T1>(bool condition, string messageToFormat, T0 p0, T1 p1)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(string.Format(StringFormatter.InvariantCulture, messageToFormat, p0, p1));
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageToFormat"/>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="messageToFormat">A string format (i.e. with {0} that will be filled with the parameters</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert<T0, T1, T2>(bool condition, string messageToFormat, T0 p0, T1 p1, T2 p2)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(string.Format(StringFormatter.InvariantCulture, messageToFormat, p0, p1, p2));
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageToFormat"/>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="messageToFormat">A string format (i.e. with {0} that will be filled with the parameters</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert<T0, T1, T2, T3>(bool condition, string messageToFormat, T0 p0, T1 p1, T2 p2, T3 p3)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(string.Format(StringFormatter.InvariantCulture, messageToFormat, p0, p1, p2, p3));
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageToFormat"/>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="messageToFormat">A string format (i.e. with {0} that will be filled with the parameters</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert<T0, T1, T2, T3, T4>(bool condition, string messageToFormat, T0 p0, T1 p1, T2 p2, T3 p3, T4 p4)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(string.Format(StringFormatter.InvariantCulture, messageToFormat, p0, p1, p2, p3, p4));
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageToFormat"/>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="messageToFormat">A string format (i.e. with {0} that will be filled with the parameters</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert<T0, T1, T2, T3, T4, T5>(bool condition, string messageToFormat, T0 p0, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(string.Format(StringFormatter.InvariantCulture, messageToFormat, p0, p1, p2, p3, p4, p5));
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageToFormat"/>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="messageToFormat">A string format (i.e. with {0} that will be filled with the parameters</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert<T0, T1, T2, T3, T4, T5, T6>(bool condition, string messageToFormat, T0 p0, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(string.Format(StringFormatter.InvariantCulture, messageToFormat, p0, p1, p2, p3, p4, p5, p6));
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageToFormat"/>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="messageToFormat">A string format (i.e. with {0} that will be filled with the parameters</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert<T0, T1, T2, T3, T4, T5, T6, T7>(bool condition, string messageToFormat, T0 p0, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(string.Format(StringFormatter.InvariantCulture, messageToFormat, p0, p1, p2, p3, p4, p5, p6, p7));
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the given message.
        /// <para/>
        /// IMPORTANT: If you need to use string concatenation when building the message, use an overload of
        /// <see cref="Assert{T0}(bool, string, T0)"/> for better performance.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="message">The message to use.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool condition, string message)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(message);
        }

        ///// <summary>
        ///// Checks for a condition; if the condition is <c>false</c>, throws an <see cref="AssertionException"/>.
        ///// </summary>
        ///// <param name="conditionFactory">A delegate that returns the conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void Assert(Func<bool> conditionFactory)
        //{
        //    if (AssertsEnabled && !conditionFactory())
        //        throw new AssertionException();
        //}

        ///// <summary>
        ///// Checks for a condition if asserts are enabled; if the <paramref name="conditionFactory"/>
        ///// returns <c>false</c>, throws an <see cref="AssertionException"/> with the message returned
        ///// from the specified <paramref name="messageFactory"/>.
        ///// </summary>
        ///// <param name="conditionFactory">A delegate that returns the conditional expression to evaluate. If the condition returned from the factory is <c>true</c>, no exception is thrown.</param>
        ///// <param name="messageFactory">A delegate to build the message to use.</param>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void Assert(Func<bool> conditionFactory, Func<string> messageFactory)
        //{
        //    if (AssertsEnabled && !conditionFactory())
        //        throw new AssertionException(messageFactory());
        //}
    }
}
