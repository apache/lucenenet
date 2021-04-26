using J2N.Text;
using Lucene.Net.Util;
using System;
using System.Diagnostics;
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
        /// <para/>
        /// IMPORTANT: For best performance, only call this method after checking to ensure the value of <see cref="AssertsEnabled"/> is <c>true</c>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool condition)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException();
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageFormat"/>.
        /// <para/>
        /// IMPORTANT: The purpose of using this overload is to defer execution of building the string until it the <paramref name="condition"/> is <c>false</c>. Ideally, we would
        /// use a <see cref="Func{String}"/> parameter, but doing so allocates extra RAM even when calls to the method are in an unreachable execution path. When passing
        /// parameters, strive to pass value or reference types without doing any pre-processing or string formatting. If necessary, wrap the parameter in another class or struct and
        /// override the <see cref="object.ToString()"/> method so any expensive formatting is deferred until after <paramref name="condition"/> is checked.
        /// <para/>
        /// IMPORTANT: For best performance, only call this method after checking to ensure the value of <see cref="AssertsEnabled"/> is <c>true</c>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="messageFormat">A composite format string to use to build a failure message.
        /// This message contains text intermixed with zero or more format items, which correspond to
        /// the <paramref name="p0"/> parameter.</param>
        /// <param name="p0">The parameter corresponding to the format item at index 0 (<c>{0}</c>).</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert<T0>(bool condition, string messageFormat, T0 p0)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(string.Format(StringFormatter.InvariantCulture, messageFormat, p0));
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageFormat"/>.
        /// <para/>
        /// IMPORTANT: The purpose of using this overload is to defer execution of building the string until it the <paramref name="condition"/> is <c>false</c>. Ideally, we would
        /// use a <see cref="Func{String}"/> parameter, but doing so allocates extra RAM even when calls to the method are in an unreachable execution path. When passing
        /// parameters, strive to pass value or reference types without doing any pre-processing or string formatting. If necessary, wrap the parameter in another class or struct and
        /// override the <see cref="object.ToString()"/> method so any expensive formatting is deferred until after <paramref name="condition"/> is checked.
        /// <para/>
        /// IMPORTANT: For best performance, only call this method after checking to ensure the value of <see cref="AssertsEnabled"/> is <c>true</c>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="messageFormat">A composite format string to use to build a failure message.
        /// This message contains text intermixed with zero or more format items, which correspond to
        /// the <paramref name="p0"/> or <paramref name="p1"/> parameters.</param>
        /// <param name="p0">The parameter corresponding to the format item at index 0 (<c>{0}</c>).</param>
        /// <param name="p1">The parameter corresponding to the format item at index 1 (<c>{1}</c>).</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert<T0, T1>(bool condition, string messageFormat, T0 p0, T1 p1)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(string.Format(StringFormatter.InvariantCulture, messageFormat, p0, p1));
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageFormat"/>.
        /// <para/>
        /// IMPORTANT: The purpose of using this overload is to defer execution of building the string until it the <paramref name="condition"/> is <c>false</c>. Ideally, we would
        /// use a <see cref="Func{String}"/> parameter, but doing so allocates extra RAM even when calls to the method are in an unreachable execution path. When passing
        /// parameters, strive to pass value or reference types without doing any pre-processing or string formatting. If necessary, wrap the parameter in another class or struct and
        /// override the <see cref="object.ToString()"/> method so any expensive formatting is deferred until after <paramref name="condition"/> is checked.
        /// <para/>
        /// IMPORTANT: For best performance, only call this method after checking to ensure the value of <see cref="AssertsEnabled"/> is <c>true</c>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="messageFormat">A composite format string to use to build a failure message.
        /// This message contains text intermixed with zero or more format items, which correspond to
        /// the <paramref name="p0"/>, <paramref name="p1"/> or <paramref name="p2"/> parameters.</param>
        /// <param name="p0">The parameter corresponding to the format item at index 0 (<c>{0}</c>).</param>
        /// <param name="p1">The parameter corresponding to the format item at index 1 (<c>{1}</c>).</param>
        /// <param name="p2">The parameter corresponding to the format item at index 2 (<c>{2}</c>).</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert<T0, T1, T2>(bool condition, string messageFormat, T0 p0, T1 p1, T2 p2)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(string.Format(StringFormatter.InvariantCulture, messageFormat, p0, p1, p2));
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageFormat"/>.
        /// <para/>
        /// IMPORTANT: The purpose of using this overload is to defer execution of building the string until it the <paramref name="condition"/> is <c>false</c>. Ideally, we would
        /// use a <see cref="Func{String}"/> parameter, but doing so allocates extra RAM even when calls to the method are in an unreachable execution path. When passing
        /// parameters, strive to pass value or reference types without doing any pre-processing or string formatting. If necessary, wrap the parameter in another class or struct and
        /// override the <see cref="object.ToString()"/> method so any expensive formatting is deferred until after <paramref name="condition"/> is checked.
        /// <para/>
        /// IMPORTANT: For best performance, only call this method after checking to ensure the value of <see cref="AssertsEnabled"/> is <c>true</c>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="messageFormat">A composite format string to use to build a failure message.
        /// This message contains text intermixed with zero or more format items, which correspond to
        /// the <paramref name="p0"/>, <paramref name="p1"/>, <paramref name="p2"/> or <paramref name="p3"/> parameters.</param>
        /// <param name="p0">The parameter corresponding to the format item at index 0 (<c>{0}</c>).</param>
        /// <param name="p1">The parameter corresponding to the format item at index 1 (<c>{1}</c>).</param>
        /// <param name="p2">The parameter corresponding to the format item at index 2 (<c>{2}</c>).</param>
        /// <param name="p3">The parameter corresponding to the format item at index 3 (<c>{3}</c>).</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert<T0, T1, T2, T3>(bool condition, string messageFormat, T0 p0, T1 p1, T2 p2, T3 p3)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(string.Format(StringFormatter.InvariantCulture, messageFormat, p0, p1, p2, p3));
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageFormat"/>.
        /// <para/>
        /// IMPORTANT: The purpose of using this overload is to defer execution of building the string until it the <paramref name="condition"/> is <c>false</c>. Ideally, we would
        /// use a <see cref="Func{String}"/> parameter, but doing so allocates extra RAM even when calls to the method are in an unreachable execution path. When passing
        /// parameters, strive to pass value or reference types without doing any pre-processing or string formatting. If necessary, wrap the parameter in another class or struct and
        /// override the <see cref="object.ToString()"/> method so any expensive formatting is deferred until after <paramref name="condition"/> is checked.
        /// <para/>
        /// IMPORTANT: For best performance, only call this method after checking to ensure the value of <see cref="AssertsEnabled"/> is <c>true</c>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="messageFormat">A composite format string to use to build a failure message.
        /// This message contains text intermixed with zero or more format items, which correspond to
        /// the <paramref name="p0"/>, <paramref name="p1"/>, <paramref name="p2"/>, <paramref name="p3"/>
        /// or <paramref name="p4"/> parameters.</param>
        /// <param name="p0">The parameter corresponding to the format item at index 0 (<c>{0}</c>).</param>
        /// <param name="p1">The parameter corresponding to the format item at index 1 (<c>{1}</c>).</param>
        /// <param name="p2">The parameter corresponding to the format item at index 2 (<c>{2}</c>).</param>
        /// <param name="p3">The parameter corresponding to the format item at index 3 (<c>{3}</c>).</param>
        /// <param name="p4">The parameter corresponding to the format item at index 4 (<c>{4}</c>).</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert<T0, T1, T2, T3, T4>(bool condition, string messageFormat, T0 p0, T1 p1, T2 p2, T3 p3, T4 p4)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(string.Format(StringFormatter.InvariantCulture, messageFormat, p0, p1, p2, p3, p4));
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageFormat"/>.
        /// <para/>
        /// IMPORTANT: The purpose of using this overload is to defer execution of building the string until it the <paramref name="condition"/> is <c>false</c>. Ideally, we would
        /// use a <see cref="Func{String}"/> parameter, but doing so allocates extra RAM even when calls to the method are in an unreachable execution path. When passing
        /// parameters, strive to pass value or reference types without doing any pre-processing or string formatting. If necessary, wrap the parameter in another class or struct and
        /// override the <see cref="object.ToString()"/> method so any expensive formatting is deferred until after <paramref name="condition"/> is checked.
        /// <para/>
        /// IMPORTANT: For best performance, only call this method after checking to ensure the value of <see cref="AssertsEnabled"/> is <c>true</c>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="messageFormat">A composite format string to use to build a failure message.
        /// This message contains text intermixed with zero or more format items, which correspond to
        /// the <paramref name="p0"/>, <paramref name="p1"/>, <paramref name="p2"/>, <paramref name="p3"/>,
        /// <paramref name="p4"/> or <paramref name="p5"/> parameters.</param>
        /// <param name="p0">The parameter corresponding to the format item at index 0 (<c>{0}</c>).</param>
        /// <param name="p1">The parameter corresponding to the format item at index 1 (<c>{1}</c>).</param>
        /// <param name="p2">The parameter corresponding to the format item at index 2 (<c>{2}</c>).</param>
        /// <param name="p3">The parameter corresponding to the format item at index 3 (<c>{3}</c>).</param>
        /// <param name="p4">The parameter corresponding to the format item at index 4 (<c>{4}</c>).</param>
        /// <param name="p5">The parameter corresponding to the format item at index 5 (<c>{5}</c>).</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert<T0, T1, T2, T3, T4, T5>(bool condition, string messageFormat, T0 p0, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(string.Format(StringFormatter.InvariantCulture, messageFormat, p0, p1, p2, p3, p4, p5));
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageFormat"/>.
        /// <para/>
        /// IMPORTANT: The purpose of using this overload is to defer execution of building the string until it the <paramref name="condition"/> is <c>false</c>. Ideally, we would
        /// use a <see cref="Func{String}"/> parameter, but doing so allocates extra RAM even when calls to the method are in an unreachable execution path. When passing
        /// parameters, strive to pass value or reference types without doing any pre-processing or string formatting. If necessary, wrap the parameter in another class or struct and
        /// override the <see cref="object.ToString()"/> method so any expensive formatting is deferred until after <paramref name="condition"/> is checked.
        /// <para/>
        /// IMPORTANT: For best performance, only call this method after checking to ensure the value of <see cref="AssertsEnabled"/> is <c>true</c>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="messageFormat">A composite format string to use to build a failure message.
        /// This message contains text intermixed with zero or more format items, which correspond to
        /// the <paramref name="p0"/>, <paramref name="p1"/>, <paramref name="p2"/>, <paramref name="p3"/>,
        /// <paramref name="p4"/>, <paramref name="p5"/> or <paramref name="p6"/> parameters.</param>
        /// <param name="p0">The parameter corresponding to the format item at index 0 (<c>{0}</c>).</param>
        /// <param name="p1">The parameter corresponding to the format item at index 1 (<c>{1}</c>).</param>
        /// <param name="p2">The parameter corresponding to the format item at index 2 (<c>{2}</c>).</param>
        /// <param name="p3">The parameter corresponding to the format item at index 3 (<c>{3}</c>).</param>
        /// <param name="p4">The parameter corresponding to the format item at index 4 (<c>{4}</c>).</param>
        /// <param name="p5">The parameter corresponding to the format item at index 5 (<c>{5}</c>).</param>
        /// <param name="p6">The parameter corresponding to the format item at index 6 (<c>{6}</c>).</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert<T0, T1, T2, T3, T4, T5, T6>(bool condition, string messageFormat, T0 p0, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(string.Format(StringFormatter.InvariantCulture, messageFormat, p0, p1, p2, p3, p4, p5, p6));
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageFormat"/>.
        /// <para/>
        /// IMPORTANT: The purpose of using this overload is to defer execution of building the string until it the <paramref name="condition"/> is <c>false</c>. Ideally, we would
        /// use a <see cref="Func{String}"/> parameter, but doing so allocates extra RAM even when calls to the method are in an unreachable execution path. When passing
        /// parameters, strive to pass value or reference types without doing any pre-processing or string formatting. If necessary, wrap the parameter in another class or struct and
        /// override the <see cref="object.ToString()"/> method so any expensive formatting is deferred until after <paramref name="condition"/> is checked.
        /// <para/>
        /// IMPORTANT: For best performance, only call this method after checking to ensure the value of <see cref="AssertsEnabled"/> is <c>true</c>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="messageFormat">A composite format string to use to build a failure message.
        /// This message contains text intermixed with zero or more format items, which correspond to
        /// the <paramref name="p0"/>, <paramref name="p1"/>, <paramref name="p2"/>, <paramref name="p3"/>,
        /// <paramref name="p4"/>, <paramref name="p5"/>, <paramref name="p6"/> or <paramref name="p7"/> parameters.</param>
        /// <param name="p0">The parameter corresponding to the format item at index 0 (<c>{0}</c>).</param>
        /// <param name="p1">The parameter corresponding to the format item at index 1 (<c>{1}</c>).</param>
        /// <param name="p2">The parameter corresponding to the format item at index 2 (<c>{2}</c>).</param>
        /// <param name="p3">The parameter corresponding to the format item at index 3 (<c>{3}</c>).</param>
        /// <param name="p4">The parameter corresponding to the format item at index 4 (<c>{4}</c>).</param>
        /// <param name="p5">The parameter corresponding to the format item at index 5 (<c>{5}</c>).</param>
        /// <param name="p6">The parameter corresponding to the format item at index 6 (<c>{6}</c>).</param>
        /// <param name="p7">The parameter corresponding to the format item at index 7 (<c>{7}</c>).</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert<T0, T1, T2, T3, T4, T5, T6, T7>(bool condition, string messageFormat, T0 p0, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7)
        {
            if (AssertsEnabled && !condition)
                throw new AssertionException(string.Format(StringFormatter.InvariantCulture, messageFormat, p0, p1, p2, p3, p4, p5, p6, p7));
        }

        /// <summary>
        /// Checks for a condition; if the <paramref name="condition"/> is <c>false</c>, throws an <see cref="AssertionException"/> with the given message.
        /// <para/>
        /// IMPORTANT: If you need to use string concatenation when building the message, use an overload of
        /// <see cref="Assert{T0}(bool, string, T0)"/> for better performance.
        /// <para/>
        /// IMPORTANT: For best performance, only call this method after checking to ensure the value of <see cref="AssertsEnabled"/> is <c>true</c>.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, no exception is thrown.</param>
        /// <param name="message">The message to use to indicate a failure of <paramref name="condition"/>.</param>
        [DebuggerStepThrough]
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
        //[DebuggerStepThrough]
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
        //[DebuggerStepThrough]
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void Assert(Func<bool> conditionFactory, Func<string> messageFactory)
        //{
        //    if (AssertsEnabled && !conditionFactory())
        //        throw new AssertionException(messageFactory());
        //}
    }
}
