using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Lucene.Net.Search.Suggest
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
    /// Average with standard deviation.
    /// </summary>
    internal class Average
    {
        /// <summary>
        /// Average (in milliseconds).
        /// </summary>
        public readonly double avg;

        /// <summary>
        /// Standard deviation (in milliseconds).
        /// </summary>
        public readonly double stddev;


        Average(double avg, double stddev)
        {
            this.avg = avg;
            this.stddev = stddev;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:#.0} [+- {1:#.00}]" /*"%.0f [+- %.2f]"*/,
                avg, stddev);
        }

        internal static Average From(IEnumerable<double> values)
        {
            double sum = 0;
            double sumSquares = 0;

            foreach (double l in values)
            {
                sum += l;
                sumSquares += l * l;
            }

            double avg = sum / (double)values.Count();
            return new Average(
                (sum / (double)values.Count()),
                Math.Sqrt(sumSquares / (double)values.Count() - avg * avg));
        }
    }
}
