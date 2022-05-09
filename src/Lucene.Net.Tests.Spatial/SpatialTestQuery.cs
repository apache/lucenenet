using J2N.Text;
using Lucene.Net.Spatial.Queries;
using Spatial4n.Context;
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
    /// Helper class to execute queries
    /// </summary>
    public class SpatialTestQuery
    {
        public string testname;
        public string line;
        public int lineNumber = -1;
        public SpatialArgs args;
        public IList<string> ids = new JCG.List<string>();

        /**
         * Get Test Queries.  The InputStream is closed.
         */
        public static IEnumerator<SpatialTestQuery> GetTestQueries(
            SpatialArgsParser parser,
            SpatialContext ctx,
            string name,
            Stream @in)
        {

            IList<SpatialTestQuery> results = new JCG.List<SpatialTestQuery>();

            TextReader bufInput = new StreamReader(@in, Encoding.UTF8);
            try
            {
                String line;
                for (int lineNumber = 1; (line = bufInput.ReadLine()) != null; lineNumber++)
                {
                    SpatialTestQuery test = new SpatialTestQuery();
                    test.line = line;
                    test.lineNumber = lineNumber;

                    try
                    {
                        // skip a comment
                        if (line.StartsWith("[", StringComparison.Ordinal))
                        {
                            int idx2 = line.IndexOf(']');
                            if (idx2 > 0)
                            {
                                line = line.Substring(idx2 + 1);
                            }
                        }

                        int idx = line.IndexOf('@');
                        StringTokenizer st = new StringTokenizer(line.Substring(0, idx - 0));
                        while (st.MoveNext())
                        {
                            test.ids.Add(st.Current.Trim());
                        }
                        test.args = parser.Parse(line.Substring(idx + 1).Trim(), ctx);
                        results.Add(test);
                    }
                    catch (Exception ex)
                    {
                        throw RuntimeException.Create("invalid query line: " + test.line, ex);
                    }
                }
            }
            finally
            {
                bufInput.Dispose();
            }
            return results.GetEnumerator();
        }

        public override String ToString()
        {
            if (line != null)
                return line;
            return args.toString() + " " + ids;
        }
    }
}
