namespace Lucene.Net.Expressions.Benchmarks
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

    /// <summary>The expressions the benchmarks parse and compile, by size.</summary>
    public static class Expressions
    {
        public const string Small = "_score * 2";

        public const string Medium = "0.3 * ln(popularity + 1) + 0.7 * _score - haversin(lat, lon, 40.7, -74.0) / 100";

        private const string LargePart =
            "(popularity > 1000 ? ln(popularity) : sqrt(popularity)) * 0.25"
            + " + (_score >= 0.5 && recency < 30 ? 2 * _score : _score / 2)"
            + " + max(min(clicks / (views + 1), 1), 0) * 0.15"
            + " - (recency >> 2 & 0xFF) / 255"
            + " + abs(cos(lat) * sin(lon)) * 0.01"
            + " + (views != 0 || clicks == 1 ? pow(_score, 2) : 0)"
            + " + atan2(lat, lon) / 3.14159 + logn(10, 1e3 + views) - (~recency & 017) * 1e-3";

        public static readonly string Large = "(" + LargePart + ") * 0.5 + (" + LargePart + ") * 0.3 + (" + LargePart + ") * 0.2";

        public static string Get(string size)
        {
            return size switch
            {
                nameof(Small) => Small,
                nameof(Medium) => Medium,
                _ => Large,
            };
        }
    }
}
