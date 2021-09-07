using Lucene.Net.QueryParsers.Surround.Parser;
using System;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.QueryParsers.Surround.Query
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

    public class ExceptionQueryTst
    {
        private string queryText;
        private bool verbose;

        public ExceptionQueryTst(string queryText, bool verbose)
        {
            this.queryText = queryText;
            this.verbose = verbose;
        }

        public void DoTest(StringBuilder failQueries)
        {
            bool pass = false;
            SrndQuery lq = null;
            try
            {
                lq = Parser.QueryParser.Parse(queryText);
                if (verbose)
                {
                    Console.WriteLine("Query: " + queryText + "\nParsed as: " + lq.ToString());
                }
            }
            catch (Lucene.Net.QueryParsers.Surround.Parser.ParseException e)
            {
                if (verbose)
                {
                    Console.WriteLine("Parse exception for query:\n"
                                      + queryText + "\n"
                                      + e.Message);
                }
                pass = true;
            }
            if (!pass)
            {
                failQueries.append(queryText);
                failQueries.append("\nParsed as: ");
                failQueries.append(lq.toString());
                failQueries.append("\n");
            }
        }

        public static string GetFailQueries(string[] exceptionQueries, bool verbose)
        {
            StringBuilder failQueries = new StringBuilder();
            for (int i = 0; i < exceptionQueries.Length; i++)
            {
                new ExceptionQueryTst(exceptionQueries[i], verbose).DoTest(failQueries);
            }
            return failQueries.toString();
        }
    }
}
