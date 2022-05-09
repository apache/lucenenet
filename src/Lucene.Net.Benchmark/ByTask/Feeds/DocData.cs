using Lucene.Net.Documents;
using System;
using System.Collections.Generic;

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
    /// Output of parsing (e.g. HTML parsing) of an input document.
    /// </summary>
    public class DocData
    {
        public string Name { get; set; }
        public string Body { get; set; }
        public string Title { get; set; }
        private string date;
        public int ID { get; set; }
        public IDictionary<string, string> Props { get; set; }

        public void Clear()
        {
            Name = null;
            Body = null;
            Title = null;
            date = null;
            Props = null;
            ID = -1;
        }

        /// <summary>
        /// Gets the date. If the ctor with <see cref="DateTime"/> was called, then the string
        /// returned is the output of <see cref="DateTools.DateToString(DateTime, DateResolution)"/>.
        /// Otherwise it's the string passed to the other ctor.
        /// </summary>
        public virtual string Date => date;

        public virtual void SetDate(DateTime? date)
        {
            if (date.HasValue)
            {
                SetDate(DateTools.DateToString(date.Value, DateResolution.SECOND));
            }
            else
            {
                this.date = null;
            }
        }

        public virtual void SetDate(string date)
        {
            this.date = date;
        }
    }
}
