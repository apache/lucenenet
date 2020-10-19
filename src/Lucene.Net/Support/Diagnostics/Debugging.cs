using Lucene.Net.Util;
using System;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ShouldAssert(bool condition)
        {
            return AssertsEnabled && condition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowAssertIf(bool condition)
        {
            if (AssertsEnabled && condition)
            {
                ThrowAssert();
            }
        }

        /// <summary>
        /// Checks for a condition; if the condition is <c>false</c>, throws an <see cref="AssertionException"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowAssert()
        {
            throw new AssertionException();
        }

        /// <summary>
        /// Throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageToFormat"/>.
        /// </summary>
        /// <param name="messageToFormat">A string format (i.e. with {0} that will be filled with the parameters)</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowAssert<T0>(string messageToFormat, T0 p0)
        {
            throw new AssertionException(string.Format(messageToFormat, p0));
        }

        /// <summary>
        /// Throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageToFormat"/>.
        /// </summary>
        /// <param name="messageToFormat">A string format (i.e. with {0} that will be filled with the parameters)</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowAssert<T0, T1>(string messageToFormat, T0 p0, T1 p1)
        {
            throw new AssertionException(string.Format(messageToFormat, p0, p1));
        }

        /// <summary>
        /// Throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageToFormat"/>.
        /// </summary>
        /// <param name="messageToFormat">A string format (i.e. with {0} that will be filled with the parameters)</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowAssert<T0, T1, T2>(string messageToFormat, T0 p0, T1 p1, T2 p2)
        {
            throw new AssertionException(string.Format(messageToFormat, p0, p1, p2));
        }

        /// <summary>
        /// Throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageToFormat"/>.
        /// </summary>
        /// <param name="messageToFormat">A string format (i.e. with {0} that will be filled with the parameters)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowAssert<T0, T1, T2, T3>(string messageToFormat, T0 p0, T1 p1, T2 p2, T3 p3)
        {
            throw new AssertionException(string.Format(messageToFormat, p0, p1, p2, p3));
        }

        /// <summary>
        /// Throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageToFormat"/>.
        /// </summary>
        /// <param name="messageToFormat">A string format (i.e. with {0} that will be filled with the parameters)</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowAssert<T0, T1, T2, T3, T4>(string messageToFormat, T0 p0, T1 p1, T2 p2, T3 p3, T4 p4)
        {
            throw new AssertionException(string.Format(messageToFormat, p0, p1, p2, p3, p4));
        }

        /// <summary>
        /// Throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageToFormat"/>.
        /// </summary>
        /// <param name="messageToFormat">A string format (i.e. with {0} that will be filled with the parameters)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowAssert<T0, T1, T2, T3, T4, T5>(string messageToFormat, T0 p0, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5)
        {
            throw new AssertionException(string.Format(messageToFormat, p0, p1, p2, p3, p4, p5));
        }

        /// <summary>
        /// Throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageToFormat"/>.
        /// </summary>
        /// <param name="messageToFormat">A string format (i.e. with {0} that will be filled with the parameters)</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowAssert<T0, T1, T2, T3, T4, T5, T6>(string messageToFormat, T0 p0, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6)
        {
            throw new AssertionException(string.Format(messageToFormat, p0, p1, p2, p3, p4, p5, p6));
        }

        /// <summary>
        /// Throws an <see cref="AssertionException"/> with the message formated 
        /// from the specified <paramref name="messageToFormat"/>.
        /// </summary>
        /// <param name="messageToFormat">A string format (i.e. with {0} that will be filled with the parameters)</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowAssert<T0, T1, T2, T3, T4, T5, T6, T7>(string messageToFormat, T0 p0, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7)
        {
            throw new AssertionException(string.Format(messageToFormat, p0, p1, p2, p3, p4, p5, p6, p7));
        }

        /// <summary>
        /// Throws an <see cref="AssertionException"/> with the given message.
        /// <para/>
        /// </summary>
        /// <param name="message">The message to use.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowAssert(string message)
        {
            throw new AssertionException(message);
        }
    }
}
