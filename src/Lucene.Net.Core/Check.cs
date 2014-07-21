/**
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
 */

namespace Lucene.Net
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Checks for code for issues that causes an error. Think of it as the replacement for Guard.
    /// </summary>
    internal static class Check
    {

        [DebuggerStepThrough]
        public static void InRangeOfLength(int start, int count, int length)
        {
            if (start < 0 || start > length || count > length || start > count)
            {
                var message = string.Format("The argument, start, must not be less than 0 or " +
                    " greater than end or Length. The argument, count, must be equal to or less than Length. " +
                    " Start was {0}. Count was {1}. Length was {2}", start, count, length);

                throw new IndexOutOfRangeException(message);
            }
        }

        [DebuggerStepThrough]
        public static void InRangeOfLength(string argument, int value, int length)
        {
            if (value < 0 || value > length)
            {
                var message = string.Format("{0} must not be less than 0 or " +
                    "greater than or equal to the Length, {1}. {0} was {2}", argument, length, value);

                throw new IndexOutOfRangeException(message);
            }
        }

        [DebuggerStepThrough]
        public static T NotNull<T>(string argument, T value, bool reference = false)
        {
            if (value == null)
            {
                if (reference)
                    throw new NullReferenceException(argument + " cannot be null.");
                else
                    throw new ArgumentNullException(argument);

            }

            return value;
        }

        [DebuggerStepThrough]
        public static string NotEmpty(string argument, string value, bool reference = false)
        {
            Check.NotNull(argument, value, reference);

            if (value.Length == 0)
            {
                throw new ArgumentException(string.Format("Argument, {0}, must not be empty.", argument), argument);
            }

            return value;
        }

        [DebuggerStepThrough]
        public static string NotEmptyOrWhitespace(string argument, string value, bool reference = false)
        {
            Check.NotEmpty(argument, value, reference);

            if (value.Trim().Length == 0)
            {
                throw new ArgumentException(string.Format("Argument, {0}, is whitespace. {0} requires a value.", argument), argument);
            }

            return value;
        }
    }
}