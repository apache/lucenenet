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

namespace Lucene.Net.Support
{
    /// <summary>
    /// A readable sequence of <see cref="System.Char"/> values.
    /// </summary>
    public interface ICharSequence
    {
        /// <summary>
        /// The number of characters in the sequence.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Returns the char at specified index.
        /// </summary>
        /// <param name="index">The index of the char to be returned.</param>
        /// <returns>A char</returns>
        char CharAt(int index);

        /// <summary>
        /// Returns a new <see cref="ICharSequence"/> of the specified range of start and end.
        /// </summary>
        /// <param name="start">The position to start the new sequence.</param>
        /// <param name="end">The position to end the new sequence.</param>
        /// <returns>A new <see cref="ICharSequence"/>.</returns>
        ICharSequence SubSequence(int start, int end);
    }
}
