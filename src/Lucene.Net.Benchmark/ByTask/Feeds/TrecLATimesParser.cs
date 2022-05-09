﻿using System;
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
    /// Parser for the FT docs in trec disks 4+5 collection format
    /// </summary>
    public class TrecLATimesParser : TrecDocParser
    {
        private const string DATE = "<DATE>";
        private const string DATE_END = "</DATE>";
        private const string DATE_NOISE = "day,"; // anything aftre the ',' 

        private const string SUBJECT = "<SUBJECT>";
        private const string SUBJECT_END = "</SUBJECT>";
        private const string HEADLINE = "<HEADLINE>";
        private const string HEADLINE_END = "</HEADLINE>";

        public override DocData Parse(DocData docData, string name, TrecContentSource trecSrc,
            StringBuilder docBuf, ParsePathType pathType)
        {
            int mark = 0; // that much is skipped

            // date...
            DateTime? date = null;
            string dateStr = Extract(docBuf, DATE, DATE_END, -1, null);
            if (dateStr != null)
            {
                int d2a = dateStr.IndexOf(DATE_NOISE, StringComparison.Ordinal);
                if (d2a > 0)
                {
                    dateStr = dateStr.Substring(0, (d2a + 3) - 0); // we need the "day" part
                }
                dateStr = StripTags(dateStr, 0).ToString();
                date = trecSrc.ParseDate(dateStr.Trim());
            }

            // title... first try with SUBJECT, them with HEADLINE
            string title = Extract(docBuf, SUBJECT, SUBJECT_END, -1, null);
            if (title is null)
            {
                title = Extract(docBuf, HEADLINE, HEADLINE_END, -1, null);
            }
            if (title != null)
            {
                title = StripTags(title, 0).ToString().Trim();
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
