/*
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

using Lucene.Net.Support;
using System;

namespace Lucene.Net
{
    public static class RandomHelpers
    {
        [ExceptionToNetNumericConvention] // LUCENENET: This is for making test porting easier, keeping as-is
        public static int nextInt(this Random random, int maxValue)
        {
            return random.Next(maxValue);
        }

        [ExceptionToNetNumericConvention] // LUCENENET: This is for making test porting easier, keeping as-is
        public static int nextInt(this Random random)
        {
            return random.Next();
        }

        [ExceptionToNetNumericConvention] // LUCENENET: This is for making test porting easier, keeping as-is
        public static bool nextBoolean(this Random random)
        {
            return (random.Next(1, 100) > 50);
        }

        // http://stackoverflow.com/a/6651656
        [ExceptionToNetNumericConvention] // LUCENENET: This is for making test porting easier, keeping as-is
        public static long nextLong(this Random random)
        {
            byte[] buffer = new byte[8];
            random.NextBytes(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }

        [ExceptionToNetNumericConvention] // LUCENENET: This is for making test porting easier, keeping as-is
        public static float nextFloat(this Random random)
        {
            return (float)random.NextDouble();
        }
    }
}
