using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
    /// Extensions to the <see cref="Queue{T}"/> class.
    /// </summary>
    internal static class QueueExtensions
    {
        // Patch because .NET Framework doesn't support this
        public static bool TryDequeue<T>(this Queue<T> queue, [MaybeNullWhen(false)] out T result)
        {
            if (queue.Count > 0)
            {
                result = queue.Dequeue();
                return true;
            }
            result = default;
            return false;
        }

        // Patch because .NET Framework doesn't support this
        public static bool TryPeek<T>(this Queue<T> queue, [MaybeNullWhen(false)] out T result)
        {
            if (queue.Count > 0)
            {
                result = queue.Peek();
                return true;
            }
            result = default;
            return false;
        }
    }
}
