using J2N.Text;
using System;
using System.IO;
using System.Text;

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
    /// Parser for the GOV2 collection format
    /// </summary>
    public class TrecGov2Parser : TrecDocParser
    {
        private const string DATE = "Date: ";
        private static readonly string DATE_END = TrecContentSource.NEW_LINE;

        private const string DOCHDR = "<DOCHDR>";
        private const string TERMINATING_DOCHDR = "</DOCHDR>";

        public override DocData Parse(DocData docData, string name, TrecContentSource trecSrc,
            StringBuilder docBuf, ParsePathType pathType)
        {
            // skip some of the non-html text, optionally set date
            DateTime? date = null;
            int start = 0;
            int h1 = docBuf.IndexOf(DOCHDR, StringComparison.Ordinal);
            if (h1 >= 0)
            {
                int h2 = docBuf.IndexOf(TERMINATING_DOCHDR, h1, StringComparison.Ordinal);
                string dateStr = Extract(docBuf, DATE, DATE_END, h2, null);
                if (dateStr != null)
                {
                    date = trecSrc.ParseDate(dateStr);
                }
                start = h2 + TERMINATING_DOCHDR.Length;
            }
            string html = docBuf.ToString(start, docBuf.Length - start);
            return trecSrc.HtmlParser.Parse(docData, name, date, new StringReader(html), trecSrc);
        }
    }
}
