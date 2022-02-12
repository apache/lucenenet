using J2N.Text;
using Spatial4n.Context;
using Spatial4n.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Spatial
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
    /// This class is modelled after SpatialTestQuery.
    /// Before Lucene 4.7, this was a bit different in Spatial4n as SampleData & SampleDataReader.
    /// </summary>
    public class SpatialTestData
    {
        public String id;
        public String name;
        public IShape shape;

        /** Reads the stream, consuming a format that is a tab-separated values of 3 columns:
         * an "id", a "name" and the "shape".  Empty lines and lines starting with a '#' are skipped.
         * The stream is closed.
         */
        public static IEnumerator<SpatialTestData> GetTestData(Stream @in, SpatialContext ctx)
        {
            IList<SpatialTestData> results = new JCG.List<SpatialTestData>();
            TextReader bufInput = new StreamReader(@in, Encoding.UTF8);
            try
            {
                String line;
                while ((line = bufInput.ReadLine()) != null)
                {
                    if (line.Length == 0 || line[0] == '#')
                        continue;

                    SpatialTestData data = new SpatialTestData();
                    String[] vals = line.Split('\t').TrimEnd();
                    if (vals.Length != 3)
                        throw RuntimeException.Create("bad format; expecting 3 tab-separated values for line: " + line);
                    data.id = vals[0];
                    data.name = vals[1];
                    try
                    {
                        data.shape = ctx.ReadShapeFromWkt(vals[2]);
                    }
                    catch (Spatial4n.Exceptions.ParseException e) // LUCENENET: Spatial4n has its own ParseException that is different than the one in Support
                    {
                        throw RuntimeException.Create(e);
                    }
                    results.Add(data);
                }
            }
            finally
            {
                bufInput.Dispose();
            }
            return results.GetEnumerator();
        }
    }
}
