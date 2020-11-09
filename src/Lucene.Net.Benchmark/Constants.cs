namespace Lucene.Net.Benchmarks
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
    /// Various benchmarking constants (mostly defaults)
    /// </summary>
    public static class Constants // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        public const int DEFAULT_RUN_COUNT = 5;
        public const int DEFAULT_SCALE_UP = 5;
        public const int DEFAULT_LOG_STEP = 1000;

        public static bool[] BOOLEANS = new bool[] { false, true };

        public const int DEFAULT_MAXIMUM_DOCUMENTS = int.MaxValue;
    }
}
