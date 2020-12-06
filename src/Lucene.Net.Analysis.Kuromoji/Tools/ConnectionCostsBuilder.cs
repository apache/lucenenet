using J2N.Text;
using Lucene.Net.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Ja.Util
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

    public static class ConnectionCostsBuilder // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        private static readonly Regex whiteSpaceRegex = new Regex("\\s+", RegexOptions.Compiled);

        public static ConnectionCostsWriter Build(string filename)
        {
            using Stream inputStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            StreamReader streamReader = new StreamReader(inputStream, Encoding.ASCII);

            string line = streamReader.ReadLine();
            string[] dimensions = whiteSpaceRegex.Split(line).TrimEnd();

            if (Debugging.AssertsEnabled) Debugging.Assert(dimensions.Length == 2);

            int forwardSize = int.Parse(dimensions[0], CultureInfo.InvariantCulture);
            int backwardSize = int.Parse(dimensions[1], CultureInfo.InvariantCulture);

            if (Debugging.AssertsEnabled) Debugging.Assert(forwardSize > 0 && backwardSize > 0);

            ConnectionCostsWriter costs = new ConnectionCostsWriter(forwardSize, backwardSize);

            while ((line = streamReader.ReadLine()) != null)
            {
                string[] fields = whiteSpaceRegex.Split(line).TrimEnd();

                if (Debugging.AssertsEnabled) Debugging.Assert(fields.Length == 3);

                int forwardId = int.Parse(fields[0], CultureInfo.InvariantCulture);
                int backwardId = int.Parse(fields[1], CultureInfo.InvariantCulture);
                int cost = int.Parse(fields[2], CultureInfo.InvariantCulture);

                costs.Add(forwardId, backwardId, cost);
            }
            return costs;
        }
    }
}
