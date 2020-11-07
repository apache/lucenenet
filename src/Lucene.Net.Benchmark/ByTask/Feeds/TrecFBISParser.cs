using J2N.Text;
using System;
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
    /// Parser for the FBIS docs in trec disks 4+5 collection format
    /// </summary>
    public class TrecFBISParser : TrecDocParser
    {
        private const string HEADER = "<HEADER>";
        private const string HEADER_END = "</HEADER>";
        private static readonly int HEADER_END_LENGTH = HEADER_END.Length;

        private const string DATE1 = "<DATE1>";
        private const string DATE1_END = "</DATE1>";

        private const string TI = "<TI>";
        private const string TI_END = "</TI>";

        public override DocData Parse(DocData docData, string name, TrecContentSource trecSrc,
            StringBuilder docBuf, ParsePathType pathType)
        {
            int mark = 0; // that much is skipped
                          // optionally skip some of the text, set date, title
            DateTime? date = null;
            string title = null;
            int h1 = docBuf.IndexOf(HEADER, StringComparison.Ordinal);
            if (h1 >= 0)
            {
                int h2 = docBuf.IndexOf(HEADER_END, h1, StringComparison.Ordinal);
                mark = h2 + HEADER_END_LENGTH;
                // date...
                string dateStr = Extract(docBuf, DATE1, DATE1_END, h2, null);
                if (dateStr != null)
                {
                    date = trecSrc.ParseDate(dateStr);
                }
                // title...
                title = Extract(docBuf, TI, TI_END, h2, null);
            }
            docData.Clear();
            docData.Name = name;
            docData.SetDate(date);
            docData.Title = title;
            docData.Body = StripTags(docBuf, mark).ToString();
            return docData;
        }
    }
}
