using System;

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
    /// Extensions to <see cref="Random"/> in order to randomly generate
    /// types other than <see cref="int"/> or <see cref="double"/>.
    /// </summary>
    public static class RandomExtensions
    {
        /// <summary>
        /// Generates a random <see cref="bool"/>, with a random distribution of
        /// approximately 50/50.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <returns>A random <see cref="bool"/>.</returns>
        public static bool NextBoolean(this Random random)
        {
            return (random.Next(1, 100) > 50);
        }

        /// <summary>
        /// Generates a random <see cref="long"/>.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <returns>A random <see cref="long"/>.</returns>
        // http://stackoverflow.com/a/6651656
        public static long NextInt64(this Random random)
        {
            byte[] buffer = new byte[8];
            random.NextBytes(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }

        /// <summary>
        /// Generates a random <see cref="float"/>.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <returns>A random <see cref="float"/>.</returns>
        public static float NextSingle(this Random random)
        {
            return (float)random.NextDouble();
        }
    }
}
