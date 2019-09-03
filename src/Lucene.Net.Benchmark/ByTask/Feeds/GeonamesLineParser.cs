using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
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
    /// A line parser for Geonames.org data.
    /// See <a href="http://download.geonames.org/export/dump/readme.txt">'geoname' table</a>.
    /// Requires <see cref="SpatialDocMaker"/>.
    /// </summary>
    public class GeonamesLineParser : LineParser
    {
        /// <summary>
        /// This header will be ignored; the geonames format is fixed and doesn't have a header line.
        /// </summary>
        public GeonamesLineParser(string[] header)
            : base(header)
        {
        }

        public override void ParseLine(DocData docData, string line)
        {
            string[] parts = new Regex("\\t").Split(line, 7);//no more than first 6 fields needed

            //    Sample data line:
            // 3578267, Morne du Vitet, Morne du Vitet, 17.88333, -62.8, ...
            // ID, Name, Alternate name (unused), Lat, Lon, ...

            docData.ID = Convert.ToInt32(parts[0], CultureInfo.InvariantCulture);//note: overwrites ID assigned by LineDocSource
            docData.Name = parts[1];
            string latitude = parts[4];
            string longitude = parts[5];
            docData.Body = "POINT(" + longitude + " " + latitude + ")";//WKT is x y order
        }
    }
}
