using System;

namespace TagSoup
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

    // LUCENENET specific class to simplify adding guard clause checks to dozens of APIs with the same parameters
    internal static class Guard
    {
        public static void BufferAndRangeCheck<T>(T[] buffer, int startIndex, int length)
        {
            // Note that this is the order the Apache Harmony tests expect it to be checked in.
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, $"{nameof(startIndex)} must not be negative.");
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (startIndex > buffer.Length - length) // Checks for int overflow
                throw new ArgumentException($"{nameof(startIndex)} + {nameof(length)} may not be greater than the size of {nameof(buffer)}");
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, $"{nameof(length)} must not be negative.");
        }

    }
}
