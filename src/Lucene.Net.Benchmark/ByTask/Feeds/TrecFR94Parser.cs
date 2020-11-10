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
    /// Parser for the FR94 docs in trec disks 4+5 collection format
    /// </summary>
    public class TrecFR94Parser : TrecDocParser
    {
        private const string TEXT = "<TEXT>";
        private static readonly int TEXT_LENGTH = TEXT.Length;
        private const string TEXT_END = "</TEXT>";

        private const string DATE = "<DATE>";
        private static readonly string[] DATE_NOISE_PREFIXES = {
            "DATE:",
            "date:", //TODO improve date extraction for this format
            "t.c.",
        };
        private const string DATE_END = "</DATE>";

        //TODO can we also extract title for this format?

        public override DocData Parse(DocData docData, string name, TrecContentSource trecSrc,
            StringBuilder docBuf, ParsePathType pathType)
        {
            int mark = 0; // that much is skipped
                          // optionally skip some of the text, set date (no title?)
            DateTime? date = null;
            int h1 = docBuf.IndexOf(TEXT, StringComparison.Ordinal);
            if (h1 >= 0)
            {
                int h2 = docBuf.IndexOf(TEXT_END, h1, StringComparison.Ordinal);
                mark = h1 + TEXT_LENGTH;
                // date...
                string dateStr = Extract(docBuf, DATE, DATE_END, h2, DATE_NOISE_PREFIXES);
                if (dateStr != null)
                {
                    dateStr = StripTags(dateStr, 0).ToString();
                    date = trecSrc.ParseDate(dateStr.Trim());
                }
            }
            docData.Clear();
            docData.Name = name;
            docData.SetDate(date);
            docData.Body = StripTags(docBuf, mark).ToString();
            return docData;
        }
    }
}
